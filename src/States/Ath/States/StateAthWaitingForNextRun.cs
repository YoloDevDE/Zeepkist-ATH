using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForNextRun(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private bool _hasShownMedal;

	public override void Enter()
	{
		_hasShownMedal = false;

		if (!_hasShownMedal)
		{
			PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer?.CurrentResult;
			double lastRunTime = AthStateMachine.Ctx.LastRunTime > 0 ?
				AthStateMachine.Ctx.LastRunTime :
				currentResult?.Time ?? -1;
			bool hasMedalToShow =
				AthStateMachine.Ctx.LastRunMedalStatus is LevelStatus.AUTHOR or LevelStatus.GOLD;

			if (currentResult != null && hasMedalToShow && lastRunTime >= 0)
			{
				_hasShownMedal = true;
			}
		}

		AthStateMachine.Ctx.ResetRetries();
		AthStateMachine.SetServerMessage(true);
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthSkippingLevel(AthStateMachine));
	}

	public override void Update()
	{
		AthStateMachine.SetServerMessage(true);
	}

	public override void OnRoundStarted()
	{
		StateMachine.TransitionTo(new StateAthWaitingForFinish(AthStateMachine));
	}

	public override void OnPhotoModeEntered()
	{
		if (!GameStateObserver.IsRacing)
		{
			Logger.LogInfo(
				"StateAthWaitingForNextRun: Photo mode entered outside a running race, keeping the clock paused.");
			return;
		}

		OnRoundStarted();
	}

	public override void OnPlayerSpawned()
	{
	}
}
