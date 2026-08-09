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
using ZeepSDK.Racing;
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
	private const string _lobbyName = "Author Time Hunting";

	private const int _maxPlayers = 16;

	/// <summary>A hunt is a lobby of one, so it is never public - at creation and again afterwards.</summary>
	private const bool _isPublic = false;

	private const string _gameScene = "GameScene";

	private static readonly TimeSpan _timeout = TimeSpan.FromSeconds(30);

	/// <summary>The first level of a fresh lobby still has to be downloaded and built.</summary>
	private static readonly TimeSpan _levelTimeout = TimeSpan.FromSeconds(60);

	/// <summary>
	///     A lobby that just opened is not ready to be taken over: the playlist is still the default
	///     one, the round is running on the server's clock and the host powers have only just
	///     landed. What says that setup is finished is the round starting, so that is what is waited
	///     for - a fixed ten seconds was a guess that was either a stall or a race, and usually
	///     both.
	/// </summary>
	private static readonly TimeSpan _roundTimeout = TimeSpan.FromSeconds(60);

	private readonly CancellationTokenSource _cts = new();
	private readonly IGamemode _gamemode;

	private bool _left;

	private bool _roundStarted;

	public StateMasterCreatingLobby(AthMasterController controller, IGamemode gamemode) : base(controller)
	{
		_gamemode = gamemode;
	}

	private AthMasterController AthMaster => (AthMasterController)Controller;

	public override async void Enter()
	{
		AthRequests.StopRequested += Cancel;
		RacingApi.RoundStarted += OnRoundStarted;

		AthMaster.Services.Silence.Silence();
		AthMaster.Services.Loading.Show("Opening a private lobby");
		AthMaster.Services.Loading.ShowWelcome(_gamemode);
		AthMaster.Services.Loading.Step("Creating lobby");

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
		RacingApi.RoundStarted -= OnRoundStarted;
		AthMaster.Services.Silence.Restore();
		_cts.Cancel();
		_cts.Dispose();
	}

	/// <summary>
	///     The round the mod set up is running, which is the moment the pretence stops costing
	///     anything: from here every sound the game makes belongs to the hunt. Leaving is also
	///     unmuted, in <see cref="Exit" /> - a cancelled start that left the game silent would be
	///     the worst bug in the mod.
	/// </summary>
	private void OnRoundStarted()
	{
		_roundStarted = true;
		AthMaster.Services.Silence.Restore();
	}

	private static void HideFromTheRoomList()
	{
		ZeepkistLobby lobby = ZeepkistNetwork.CurrentLobby;

		if (lobby == null || !lobby.IsPublic)
		{
			return;
		}

		lobby.IsPublic = _isPublic;

		ZeepkistNetwork.NetworkClient?.SendPacket(new ChangeLobbyVisibilityPacket { Visiblity = _isPublic });
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

		SceneManager.LoadScene(_gameScene);
	}

	private async Task<bool> Create()
	{
		if (!ZeepkistNetwork.IsConnected)
		{
			return false;
		}

		ZeepkistNetwork.CreateLobby(_lobbyName, _maxPlayers, _isPublic);

		if (!await Wait.UntilAsync(() => ZeepkistNetwork.IsConnectedToGame, _timeout, _cts.Token))
		{
			return false;
		}

		HideFromTheRoomList();
		AthMaster.Services.Loading.Step("Loading the lobby level");
		EnterTheGameScene();

		if (!await Wait.UntilAsync(() => GameStateObserver.IsLevelReady, _levelTimeout, _cts.Token))
		{
			Logger.LogWarning("StateMasterCreatingLobby: The lobby is up but no level is running yet.");
		}

		AthMaster.Services.Loading.Step("Waiting for the round");

		if (!await Wait.UntilAsync(() => _roundStarted, _roundTimeout, _cts.Token))
		{
			Logger.LogWarning("StateMasterCreatingLobby: No round start arrived, handing over anyway.");
		}

		AthMaster.Services.Loading.Step("Fetching levels");

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
			AthMaster.Services.Loading.Hide();
			FrogNotification.Error("Could not open a lobby, the hunt did not start");
			Controller.TransitionTo(new StateMasterOff(AthMaster));

			return;
		}

		Controller.TransitionTo(new StateMasterOff(AthMaster, _gamemode));
	}

	private void Cancel()
	{
		AthMaster.Services.Loading.Hide();
		FrogNotification.Info("Start cancelled");
		Controller.TransitionTo(new StateMasterOff(AthMaster));
	}
}
