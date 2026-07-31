using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;
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
			Overlay.Notify("Author time claimed!<br>[Respawn to continue]", HudPalette.Default);

			double lastRunTime = AthStateMachine.Ctx.LastRunTime;

			if (lastRunTime < 0)
			{
				lastRunTime = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1;
			}

			if (lastRunTime >= 0)
			{
				Overlay.ShowBanner(MedalBanner.ForRun(AthStateMachine.Ctx.CurrentLevel, lastRunTime,
					AthStateMachine.Ctx.LastRunMedalWasNew));
			}
			else
			{
				Overlay.ShowBanner(MedalBanner.Message("NEW medal: AUTHOR  (respawn to skip)", 6f));
			}

			AthStateMachine.Show(AthStateMachine.Ctx.Messages.AuthorMedalClaimed());
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
		Overlay.ClearBanner();
		StateMachine.TransitionTo(new StateAthLevelSummary(AthStateMachine));
	}


	// Private Methods
	public override void OnPlayerSpawned()
	{
		Overlay.ClearBanner();
		PlaylistService.SkipLevel();
	}
}