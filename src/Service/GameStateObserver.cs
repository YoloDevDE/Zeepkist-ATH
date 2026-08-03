using System;
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
public partial class GameStateObserver
{
	private ObserverBehaviour _behaviour;
	private bool _lastIsRacing;
	private bool _lastKnownStateValid;

	/// <summary>
	///     Nullable, because "no lobby" is a state the observer has to be able to remember. It
	///     used to be stored as Racing, so every frame outside a lobby compared unequal to the
	///     null it had just read and reported a change that had not happened - a few thousand
	///     identical lines per session.
	/// </summary>
	private ZeepkistLobbyState? _lastLobbyState;

	public GameStateObserver()
	{
		GameObject host = new(nameof(GameStateObserver)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<ObserverBehaviour>();
		_behaviour.Bind(this);
	}

	/// <summary>True while connected to an online lobby.</summary>
	public static bool IsInOnlineLobby => ZeepkistNetwork.CurrentLobby != null;

	/// <summary>
	///     The lobby's reported state, or null when there is no lobby. Remember that
	///     <see cref="ZeepkistLobbyState.Racing" /> is also what an unreported lobby says -
	///     prefer <see cref="IsRacing" /> for "may we act now".
	/// </summary>
	public static ZeepkistLobbyState? LobbyState =>
		ZeepkistNetwork.CurrentLobby == null ? null : (ZeepkistLobbyState)ZeepkistNetwork.CurrentLobby.GameState;

	/// <summary>
	///     The one guard worth trusting: in a lobby, the lobby says racing, and the level has
	///     finished loading.
	///     The level check is what makes this different from a plain GameState comparison. The
	///     game pairs the two the same way in NetworkedZeepkistGhost before it draws anyone.
	/// </summary>
	public static bool IsRacing => IsInOnlineLobby && LobbyState == ZeepkistLobbyState.Racing && IsLevelReady;

	/// <summary>
	///     False while the game is between levels. GameMaster.loadNewLevel is set when the
	///     podium hands over to the next level and clears itself when the new GameMaster is
	///     created on scene load.
	/// </summary>
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

	/// <summary>Raised when the lobby state changes, including when a lobby is left (null).</summary>
	public event Action<ZeepkistLobbyState?> LobbyStateChanged;

	/// <summary>
	///     Raised on the frame a race actually becomes runnable. This is the signal to wait
	///     for before starting a run.
	/// </summary>
	public event Action BecameRacing;

	/// <summary>Raised on the frame a running race stops being runnable.</summary>
	public event Action StoppedRacing;

	private void Tick()
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

	/// <summary>
	///     Subscribers run inside our per-frame tick; one of them throwing must not stop the
	///     observer from reporting to the rest.
	/// </summary>
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
