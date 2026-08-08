using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.ChatCommands;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOn : StateBase
{
	private readonly CommandAthStop _stopCommand = new();

	private CommandAthBroken _brokenCommand;
	private bool _shuttingDown;

	public StateMasterOn(MasterStateMachine stateMachine, IGamemode gamemode) : base(stateMachine)
	{
		Gamemode = gamemode;
		AthStateMachine = new AthStateMachine(stateMachine.Services, gamemode);
	}

	public static bool IsActive { get; private set; }

	public IGamemode Gamemode { get; }

	public AthStateMachine AthStateMachine { get; }

	public override StateMachineBase SubStateMachine => AthStateMachine;

	private MasterStateMachine Master => (MasterStateMachine)StateMachine;

	public override void Enter()
	{
		IsActive = true;
		AthRequests.StopRequested += Stop;
		MultiplayerApi.DisconnectedFromGame += Stop;
		AthRequests.StartRequested += Start;
		AthRequests.RestartRequested += Restart;
		RacingApi.RoundStarted += OnRoundStarted;
		AthRequests.SkipBrokenRequested += SkipBrokenLevel;
		SubStateMachine.StateMachineFinished += Stop;
		ChatCommandApi.RegisterLocalChatCommand(_stopCommand);
		Master.Services.GameState.BecameRacing += RegisterBrokenCommand;
		Master.Services.GameState.StoppedRacing += UnregisterBrokenCommand;

		if (GameStateObserver.IsRacing)
		{
			RegisterBrokenCommand();
		}

		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
		Master.Services.PublishRun(AthStateMachine);
		AthStateMachine.StartTimer();
		FrogNotification.Success($"{Gamemode.DisplayName} started");
	}

	public override void Exit()
	{
		IsActive = false;
		FrogNotification.Info("Hunt stopped");
		Master.Services.PublishRun(null);
		AthStateMachine.StopTimer();
		AthStateMachine.Dispose();
		AthRequests.StopRequested -= Stop;
		MultiplayerApi.DisconnectedFromGame -= Stop;
		AthRequests.StartRequested -= Start;
		AthRequests.RestartRequested -= Restart;
		RacingApi.RoundStarted -= OnRoundStarted;
		AthRequests.SkipBrokenRequested -= SkipBrokenLevel;
		SubStateMachine.StateMachineFinished -= Stop;
		Master.Services.GameState.BecameRacing -= RegisterBrokenCommand;
		Master.Services.GameState.StoppedRacing -= UnregisterBrokenCommand;
		UnregisterBrokenCommand();
		ChatCommandApi.UnregisterLocalChatCommand(_stopCommand);
	}

	/// <summary>
	///     /ath broken exists only while a level is being raced, which is the same condition the
	///     Broken button is drawn enabled under. Between levels there is nothing to write off, so
	///     the command is gone rather than inert.
	/// </summary>
	private void RegisterBrokenCommand()
	{
		if (_brokenCommand != null)
		{
			return;
		}

		_brokenCommand = new CommandAthBroken();
		ChatCommandApi.RegisterLocalChatCommand(_brokenCommand);
	}

	private void UnregisterBrokenCommand()
	{
		if (_brokenCommand == null)
		{
			return;
		}

		ChatCommandApi.UnregisterLocalChatCommand(_brokenCommand);
		_brokenCommand = null;
	}

	private void OnRoundStarted()
	{
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
	}

	/// <summary>
	///     A restart is a new run, and a new run gets a new lobby - the same way a start does.
	///     Restarting inside the lobby the last run left behind meant inheriting its playlist and
	///     whatever was left of its round, which is the state the mod stopped trusting.
	/// </summary>
	private void Restart()
	{
		_shuttingDown = true;
		RestoreGameHud();
		StateMachine.TransitionTo(new StateMasterConnectingToServer(Master, Gamemode));
	}

	private void SkipBrokenLevel()
	{
		if (AthStateMachine.Ctx.CurrentLevel != null)
		{
			AthStateMachine.Ctx.CurrentLevel.LevelBroken = true;
			ChatApi.SendMessage("/fs");
		}
	}

	private void Start()
	{
		FrogNotification.Warn("ATH is already running");
	}

	private void Stop()
	{
		if (_shuttingDown)
		{
			return;
		}

		_shuttingDown = true;
		RestoreGameHud();
		StateMachine.TransitionTo(new StateMasterOff(Master));
	}

	private static void RestoreGameHud()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			return;
		}

		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = true;
	}
}
