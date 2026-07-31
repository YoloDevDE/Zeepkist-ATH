using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private bool _hasShownAuthorMedal;


	public override void Enter()
	{
		_hasShownAuthorMedal = false;
	}

	public override void Execute()
	{
		AthStateMachine.Ctx.CurrentLevel.Stop();

		if (!_hasShownAuthorMedal)
		{
			Messenger.Notify().Log("Author time claimed!<br>[Respawn to continue]");

			double lastRunTime = AthStateMachine.Ctx.LastRunTime;

			if (lastRunTime < 0)
			{
				lastRunTime = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1;
			}

			if (lastRunTime >= 0)
			{
			}

			_hasShownAuthorMedal = true;
		}

		AthStateMachine.SetServerMessage(true);
	}

	public override void OnAthTimerTick()
	{
		AthStateMachine.SetServerMessage(true);
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthLevelSummary(AthStateMachine));
	}


	// Private Methods
	public override void OnPlayerSpawned()
	{
		PlaylistService.SkipLevel();
	}
}