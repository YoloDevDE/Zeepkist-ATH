using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Enums;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.Level;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Watches the game and answers one question reliably: is a race actually running right
///     now? Session-scoped, runs from the moment the mod loads - ATH does not have to be
///     started for this to be current.
///     It polls rather than only listening, because the two things it combines are reported
///     differently: the lobby state arrives as an event
///     (ZeepkistNetwork.LobbyGameStateChanged) while "the level is still loading" is a plain
///     flag on GameMaster that nothing announces. Polling two booleans per frame is cheap;
///     the observer only raises events on an actual edge.
/// </summary>
public class GameStateObserver
{
	private ObserverBehaviour _behaviour;
	private bool _lastIsRacing;
	private bool _lastKnownStateValid;

	private ZeepkistLobbyState? _lastLobbyState;

	public GameStateObserver()
	{
		GameObject host = new(nameof(GameStateObserver)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<ObserverBehaviour>();
		_behaviour.Bind(this);
	}

	public static bool IsInOnlineLobby => ZeepkistNetwork.CurrentLobby != null;

	/// <summary>
	///     Whether the lobby is ours to run. Everything a hunt does to a lobby - the playlist,
	///     the round time, every skip - is a host power, so a lobby somebody else hosts is not
	///     one a hunt can happen in.
	/// </summary>
	public static bool IsLobbyHost => IsInOnlineLobby && ZeepkistNetwork.IsMasterClient;

	public static ZeepkistLobbyState? LobbyState =>
		ZeepkistNetwork.CurrentLobby == null ? null : (ZeepkistLobbyState)ZeepkistNetwork.CurrentLobby.GameState;

	public static bool IsRacing => IsInOnlineLobby && LobbyState == ZeepkistLobbyState.Racing && IsLevelReady;

	/// <summary>
	///     Whether the local player has every checkpoint of the current level. The game lets you
	///     cross the finish without them - it just refuses to score the run, shows the counter in
	///     red and calls it a dnf. ATH has to make the same distinction: a finish that does not
	///     count is not a finish, and the level's clock keeps running.
	///     A level with no checkpoints at all has nothing to miss, so racePoints of zero is
	///     always satisfied.
	/// </summary>
	public static bool AllCheckpointsPassed
	{
		get
		{
			GameMaster master = PlayerManager.Instance == null ? null : PlayerManager.Instance.currentMaster;

			if (master == null || master.playerResults == null || master.playerResults.Count == 0)
			{
				return false;
			}

			return master.playerResults[0].racepoints >= master.racePoints;
		}
	}

	/// <summary>
	///     How many checkpoints the level has. The finish is not one of them - the game counts
	///     racepoints off the checkpoint blocks alone and scores the finish separately.
	/// </summary>
	public static int CheckpointCount
	{
		get
		{
			GameMaster master = Master;

			return master == null ? 0 : master.racePoints;
		}
	}

	/// <summary>
	///     The checkpoints the current attempt has already taken. The game appends one the moment
	///     the car crosses a checkpoint trigger and starts a fresh list with every restart, so
	///     this is the attempt being driven and nothing else.
	///     Copied into plain numbers rather than handed out as the game's own split objects, so
	///     that everything downstream of it - and every test of that - can be written without the
	///     game.
	/// </summary>
	public static SplitSet CurrentSplits
	{
		get
		{
			List<WinCompare.SplitTime> splits = LocalSplits;

			if (splits == null)
			{
				return SplitSet.Empty;
			}

			double[] times = new double[splits.Count];
			double[] speeds = new double[splits.Count];

			for (int i = 0; i < splits.Count; i++)
			{
				times[i] = splits[i].time;
				speeds[i] = splits[i].velocity;
			}

			return new SplitSet(times, speeds);
		}
	}

	private static GameMaster Master => PlayerManager.Instance == null ? null : PlayerManager.Instance.currentMaster;

	/// <summary>The local player's splits, or null when there is no race to read them off.</summary>
	private static List<WinCompare.SplitTime> LocalSplits
	{
		get
		{
			GameMaster master = Master;

			if (master == null || master.playerResults == null || master.playerResults.Count == 0)
			{
				return null;
			}

			return master.playerResults[0].split_times;
		}
	}

	public static bool IsLevelReady
	{
		get
		{
			if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null)
			{
				return false;
			}

			if (PlayerManager.Instance.currentMaster.loadNewLevel)
			{
				return false;
			}

			return LevelApi.CurrentLevel != null;
		}
	}

	public event Action<ZeepkistLobbyState?> LobbyStateChanged;

	public event Action BecameRacing;

	public event Action StoppedRacing;

	public void Tick()
	{
		ZeepkistLobbyState? state = LobbyState;

		if (!_lastKnownStateValid || state != _lastLobbyState)
		{
			_lastKnownStateValid = true;
			_lastLobbyState = state;
			Logger.LogInfo(
				$"GameStateObserver: Lobby state is now {(state.HasValue ? state.Value.ToString() : "no lobby")}.");
			Raise(() => LobbyStateChanged?.Invoke(state), nameof(LobbyStateChanged));
		}

		bool racing = IsRacing;

		if (racing == _lastIsRacing)
		{
			return;
		}

		_lastIsRacing = racing;
		Logger.LogInfo($"GameStateObserver: {(racing ? "Race is running" : "Race is not running")}.");
		Raise(racing ? BecameRacing : StoppedRacing, racing ? nameof(BecameRacing) : nameof(StoppedRacing));
	}

	private static void Raise(Action raise, string eventName)
	{
		try
		{
			raise?.Invoke();
		}
		catch (Exception e)
		{
			Logger.LogError($"GameStateObserver: A subscriber of {eventName} threw: {e.Message}\n{e.StackTrace}");
		}
	}

	public void Dispose()
	{
		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}
}
