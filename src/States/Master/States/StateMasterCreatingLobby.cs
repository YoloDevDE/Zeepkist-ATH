using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine.SceneManagement;
using ZeepkistClient;
using ZeepkistNetworking;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.States.Master.States;

/// <summary>
///     Opens the lobby the hunt runs in and keeps it out of the room list. A hunt is one player
///     against the author times, and a room drawing strangers in would hand them a playlist
///     that skips a level the moment the hunter is done with it.
///     The lobby is asked to be private at creation and told again once it exists, because the
///     first is a flag on a packet that may or may not have been honoured and the second is
///     something that can be read back.
///     Getting into the lobby is the online menu's job whenever that menu is on screen - it
///     loads the game scene itself once the server answers. It is only when the lobby was made
///     from somewhere else that the scene has to be loaded here.
/// </summary>
public class StateMasterCreatingLobby : StateBase
{
	private const string LobbyName = "Author Time Hunting";

	private const int MaxPlayers = 16;

	private const bool Private = false;

	private const string GameScene = "GameScene";

	private static readonly TimeSpan Timeout = TimeSpan.FromSeconds(30);

	/// <summary>The first level of a fresh lobby still has to be downloaded and built.</summary>
	private static readonly TimeSpan LevelTimeout = TimeSpan.FromSeconds(60);

	/// <summary>
	///     A lobby that just opened is not ready to be taken over. The playlist is still the
	///     default one, the round is running on the server's clock and the host powers have only
	///     just landed - a hunt that starts into that races the lobby's own setup. So it waits,
	///     and the wait is spent telling the player what they are about to play.
	/// </summary>
	private static readonly TimeSpan Welcome = TimeSpan.FromSeconds(10);

	private readonly CancellationTokenSource _cts = new();
	private readonly IGamemode _gamemode;

	private bool _left;

	public StateMasterCreatingLobby(MasterStateMachine stateMachine, IGamemode gamemode) : base(stateMachine)
	{
		_gamemode = gamemode;
	}

	private MasterStateMachine Master => (MasterStateMachine)StateMachine;

	public override async void Enter()
	{
		AthRequests.StopRequested += Cancel;
		Master.Services.Loading.Show("Opening a private lobby");

		try
		{
			Finish(await Create());
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception e)
		{
			Logger.LogError($"StateMasterCreatingLobby: {e.Message}\n{e.StackTrace}");
			Finish(false);
		}
	}

	public override void Exit()
	{
		_left = true;
		AthRequests.StopRequested -= Cancel;
		_cts.Cancel();
		_cts.Dispose();
	}

	private static void HideFromTheRoomList()
	{
		ZeepkistLobby lobby = ZeepkistNetwork.CurrentLobby;

		if (lobby == null || !lobby.IsPublic)
		{
			return;
		}

		lobby.IsPublic = Private;

		ZeepkistNetwork.NetworkClient?.SendPacket(new ChangeLobbyVisibilityPacket { Visiblity = Private });
	}

	private static void EnterTheGameScene()
	{
		if (Object.FindObjectOfType<LobbyManager>() != null)
		{
			return;
		}

		if (PlayerManager.Instance != null)
		{
			PlayerManager.Instance.singlePlayer = true;
			PlayerManager.Instance.amountOfPlayers = 1;
		}

		SceneManager.LoadScene(GameScene);
	}

	private async Task<bool> Create()
	{
		if (!ZeepkistNetwork.IsConnected)
		{
			return false;
		}

		ZeepkistNetwork.CreateLobby(LobbyName, MaxPlayers, Private);

		if (!await Wait.UntilAsync(() => ZeepkistNetwork.IsConnectedToGame, Timeout, _cts.Token))
		{
			return false;
		}

		HideFromTheRoomList();
		Master.Services.Loading.Show("Loading the lobby");
		EnterTheGameScene();

		if (!await Wait.UntilAsync(() => GameStateObserver.IsLevelReady, LevelTimeout, _cts.Token))
		{
			Logger.LogWarning("StateMasterCreatingLobby: The lobby is up but no level is running yet.");
		}

		Master.Services.Loading.ShowWelcome(_gamemode, Welcome);
		await Task.Delay(Welcome, _cts.Token);

		return true;
	}

	private void Finish(bool created)
	{
		if (_left)
		{
			return;
		}

		if (!created)
		{
			Master.Services.Loading.Hide();
			FrogNotification.Error("Could not open a lobby, the hunt did not start");
			StateMachine.TransitionTo(new StateMasterOff(Master));

			return;
		}

		StateMachine.TransitionTo(new StateMasterOff(Master, _gamemode));
	}

	private void Cancel()
	{
		Master.Services.Loading.Hide();
		FrogNotification.Info("Start cancelled");
		StateMachine.TransitionTo(new StateMasterOff(Master));
	}
}
