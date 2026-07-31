using AuthorTimeHunting.Commands;
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

	public StateMasterOn(MasterStateMachine stateMachine) : base(stateMachine)
	{
		AthStateMachine = new AthStateMachine();
	}

	public static bool IsActive { get; private set; }

	/// <summary>The run's machine, created fresh for every /ath start and /ath restart.</summary>
	public AthStateMachine AthStateMachine { get; }

	public override StateMachineBase SubStateMachine => AthStateMachine;


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
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enabled = true;
		AthStateMachine.StartTimer();
		Messenger.Notify().Log("started");
	}

	public override void Exit()
	{
		IsActive = false;
		Messenger.Notify().Log("stopped");
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
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enabled = true;
	}


	private void Restart()
	{
		// Same reason as in Stop: the teardown below fires StateMachineFinished, and
		// an unguarded Stop would turn the restart into a plain stop.
		_shuttingDown = true;
		StateMachine.TransitionTo(new StateMasterOn((MasterStateMachine)StateMachine));
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
		Messenger.Notify().LogWarning("already started");
	}

	private void Stop()
	{
		if (_shuttingDown)
		{
			return;
		}

		_shuttingDown = true;
		RestoreGameHud();
		StateMachine.TransitionTo(new StateMasterOff((MasterStateMachine)StateMachine));
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
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enabled = false;
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.SetText("Thanks for playing ATH :)");
	}
}