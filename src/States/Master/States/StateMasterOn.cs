using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.States.Master.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOn : StateBase
{
	/// <summary>
	///     Set as soon as this instance starts tearing itself down. The teardown runs
	///     through StateAthStopping, which fires StateMachineFinished - and Stop is a
	///     subscriber of exactly that event. Without the flag an externally triggered
	///     stop re-enters TransitionTo while the first one is still on the stack, and
	///     Exit() runs twice: the "stopped" toast appears doubled and the second
	///     AthStateMachine.Dispose() touches an already destroyed component.
	/// </summary>
	private bool _shuttingDown;

	/// <param name="stateMachine">The mod's lifecycle machine.</param>
	/// <param name="gamemode">
	///     The mode this run plays by. Passed in rather than read from the registry, so a
	///     restart repeats the mode the run was started with even if the selection has since
	///     been changed.
	/// </param>
	public StateMasterOn(MasterStateMachine stateMachine, IGamemode gamemode) : base(stateMachine)
	{
		Gamemode = gamemode;
		AthStateMachine = new AthStateMachine(stateMachine.Services, gamemode);
	}

	public static bool IsActive { get; private set; }

	/// <summary>The mode the current run is being played in.</summary>
	public IGamemode Gamemode { get; }

	/// <summary>The run's machine, created fresh for every /ath start and /ath restart.</summary>
	public AthStateMachine AthStateMachine { get; }

	public override StateMachineBase SubStateMachine => AthStateMachine;

	private MasterStateMachine Master => (MasterStateMachine)StateMachine;


	/***
	MasterStateOff -- Mod is not running rn.
	MasterStateOff -> MasterStateOn
	MasterStateOn -> MasterStateOff
	MasterStateOn -- Mod is running rn.

	***/
	public override void Enter()
	{
		IsActive = true;
		CommandStop.CommandTrigger += Stop;
		MultiplayerApi.DisconnectedFromGame += Stop;
		CommandStart.CommandTrigger += Start;
		CommandRestart.CommandTrigger += Restart;
		RacingApi.RoundStarted += OnRoundStarted;
		CommandSkipBroken.CommandTrigger += SkipBrokenLevel;
		SubStateMachine.StateMachineFinished += Stop;
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
		Master.Services.PublishRun(AthStateMachine);
		AthStateMachine.StartTimer();
		ToastNotification.Success($"{Gamemode.DisplayName} started");
	}

	public override void Exit()
	{
		IsActive = false;
		ToastNotification.Info("Hunt stopped");
		Master.Services.PublishRun(null);
		AthStateMachine.StopTimer();
		AthStateMachine.Dispose();
		CommandStop.CommandTrigger -= Stop;
		MultiplayerApi.DisconnectedFromGame -= Stop;
		CommandStart.CommandTrigger -= Start;
		CommandRestart.CommandTrigger -= Restart;
		RacingApi.RoundStarted -= OnRoundStarted;
		CommandSkipBroken.CommandTrigger -= SkipBrokenLevel;
		SubStateMachine.StateMachineFinished -= Stop;
	}

	private void OnRoundStarted()
	{
		// ATH shows its own clock, so the lobby's time display stays off for the run.
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
	}


	private void Restart()
	{
		// Same reason as in Stop: the teardown below fires StateMachineFinished, and
		// an unguarded Stop would turn the restart into a plain stop.
		_shuttingDown = true;

		if (GameStateObserver.IsRacing)
		{
			StateMachine.TransitionTo(new StateMasterOn(Master, Gamemode));
			return;
		}

		// Restarting into a podium or a loading screen would have the new run's
		// StateAthStarting rewrite a playlist the game is in the middle of switching. Wind
		// this run down instead and let StateMasterOff pick the moment.
		// No race condition between the check and the transition: IsRacing is derived from
		// game state that only changes between frames, and nothing here yields.
		Logger.LogInfo("StateMasterOn: Restart requested outside a running race, deferring the new run.");
		ToastNotification.Info("ATH restarts as soon as the level is loaded");
		StateMachine.TransitionTo(new StateMasterOff(Master, Gamemode));
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
		ToastNotification.Warn("ATH is already running");
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

	/// <summary>
	///     Hands the HUD elements ATH borrowed back to the game. The whole chain is gone
	///     when the stop was triggered by DisconnectedFromGame, so it stays optional.
	/// </summary>
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