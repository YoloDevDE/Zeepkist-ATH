using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForNextRun(AthController controller) : AthState(controller)
{
	private bool _hasShownMedal;

	public override void Enter()
	{
		_hasShownMedal = false;

		if (!_hasShownMedal)
		{
			PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer?.CurrentResult;
			double lastRunTime = AthController.Ctx.LastRunTime > 0 ?
				AthController.Ctx.LastRunTime :
				currentResult?.Time ?? -1;
			bool hasMedalToShow =
				AthController.Ctx.LastRunMedalStatus is LevelStatus.Author or LevelStatus.Gold;

			if (currentResult != null && hasMedalToShow && lastRunTime >= 0)
			{
				_hasShownMedal = true;
			}
		}


		AthController.Ctx.ResetRetries();
	}

	public override void OnRoundEnded()
	{
		Controller.TransitionTo(new StateAthSkippingLevel(AthController));
	}

	public override void OnRoundStarted()
	{
		Controller.TransitionTo(new StateAthWaitingForFinish(AthController));
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
