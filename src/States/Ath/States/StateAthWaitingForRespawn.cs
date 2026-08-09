using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(AthController controller) : AthState(controller)
{
	private bool _hasShownAuthorMedal;

	public override void Enter()
	{
		_hasShownAuthorMedal = false;

		AthController.Ctx.CurrentLevel.Stop();

		if (!_hasShownAuthorMedal)
		{
			FrogNotification.Author("Author time claimed!<br>[Respawn to continue]");

			double lastRunTime = AthController.Ctx.LastRunTime;

			if (lastRunTime < 0)
			{
				lastRunTime = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1;
			}

			if (lastRunTime >= 0)
			{
			}

			_hasShownAuthorMedal = true;
		}
	}

	public override void OnRoundEnded()
	{
		Controller.TransitionTo(new StateAthLevelSummary(AthController));
	}

	public override void OnPlayerSpawned()
	{
		PlaylistService.SkipLevel();
	}
}
