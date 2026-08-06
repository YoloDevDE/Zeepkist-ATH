using System;
using System.Collections.Generic;
using AuthorTimeHunting.Enums;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Writes down what the player and the game did, so a bug report is a timeline rather than
///     a memory: every key pressed, every screen entered, and every change to the lobby's
///     playlist and its index.
///     The playlist half is the reason this exists. A run that walks into a loop does so because
///     the mod's idea of the playlist and the server's have come apart, and until now the log
///     showed only what the mod believed - never what the lobby actually held at that moment.
/// </summary>
public class TraceService
{
	private static readonly KeyCode[] Keys = (KeyCode[])Enum.GetValues(typeof(KeyCode));

	private TraceBehaviour _behaviour;
	private int _lastCount = int.MinValue;

	private int _lastIndex = int.MinValue;
	private string _lastScreen = "";
	private string _lastUid = "";

	public TraceService()
	{
		GameObject host = new(nameof(TraceService)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<TraceBehaviour>();
		_behaviour.Bind(this);

		Logger.LogInfo("TraceService: Tracing keys, screens and the lobby playlist.");
	}

	public void Tick()
	{
		TraceScreen();
		TraceKeys();
		TracePlaylist();
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

	private void TraceScreen()
	{
		string screen = Screen();

		if (screen == _lastScreen)
		{
			return;
		}

		_lastScreen = screen;
		Logger.LogInfo($"Trace: Screen is now {screen}.");
	}

	private static string Screen()
	{
		string scene = SceneManager.GetActiveScene().name;
		bool inGame = PlayerManager.Instance != null && PlayerManager.Instance.currentMaster != null;
		string lobby = ZeepkistNetwork.CurrentLobby == null ?
			"offline" :
			$"lobby in {(ZeepkistLobbyState)ZeepkistNetwork.CurrentLobby.GameState}";

		return $"'{scene}' [{(inGame ? "gameplay" : "menu")}, {lobby}]";
	}

	private static void TraceKeys()
	{
		if (!Input.anyKeyDown)
		{
			return;
		}

		try
		{
			LogPressedKeys();
		}
		catch (Exception e)
		{
			Logger.LogWarning($"TraceService: Could not read the keyboard: {e.Message}");
		}
	}

	private static void LogPressedKeys()
	{
		foreach (KeyCode key in Keys)
		{
			if (!Input.GetKeyDown(key))
			{
				continue;
			}

			Logger.LogInfo($"Trace: Key {key} pressed on screen {Screen()}.");
		}
	}

	private void TracePlaylist()
	{
		if (ZeepkistNetwork.CurrentLobby == null)
		{
			return;
		}

		List<OnlineZeeplevel> playlist = ZeepkistNetwork.CurrentLobby.Playlist;
		int index = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
		int count = playlist?.Count ?? 0;
		string uid = Uid(playlist, index);

		if (index == _lastIndex && count == _lastCount && uid == _lastUid)
		{
			return;
		}

		_lastIndex = index;
		_lastCount = count;
		_lastUid = uid;

		Logger.LogInfo($"Trace: Playlist is now {Describe(playlist, index, count)}.");
	}

	private static string Describe(List<OnlineZeeplevel> playlist, int index, int count)
	{
		if (playlist == null || index < 0 || index >= count)
		{
			return $"at index {index} of {count} entries - OUT OF RANGE";
		}

		OnlineZeeplevel level = playlist[index];

		return
			$"at index {index} of {count}: '{level.Name}' by {level.Author} (UID {level.UID}, workshop {level.WorkshopID})";
	}

	private static string Uid(List<OnlineZeeplevel> playlist, int index)
	{
		if (playlist == null || index < 0 || index >= playlist.Count)
		{
			return "";
		}

		return playlist[index].UID ?? "";
	}
}
