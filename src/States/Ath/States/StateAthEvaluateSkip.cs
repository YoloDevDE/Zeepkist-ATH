using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;

using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthEvaluateSkip(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Execute()
	{
		AthCtx athCtx = AthStateMachine.Ctx;

		if (athCtx.CurrentLevel.LevelBroken)
		{
			HandleBrokenSkip(athCtx);
		}
		else
		{
			if (athCtx.CurrentLevel.GoldMedalAcquired)
			{
				HandleGoldSkip();
			}
			else if (athCtx.AvaiableFreeSkips > 0)
			{
				HandleFreeSkip(athCtx);
			}
			else if (athCtx.IsTimeRunningLow)
			{
				HandleTimeExpiredSkip();
			}
			else
			{
				HandlePenaltySkip();
			}
		}

		athCtx.CurrentLevel.Skipped = true;
		athCtx.CurrentLevel.Stop();
		StateMachine.TransitionTo(new StateAthLevelSummary(AthStateMachine));
	}

	private void HandleBrokenSkip(AthCtx ctx)
	{
		ctx.CurrentLevel.LevelBroken = true;
		Messenger.Notify().LogWarning("'Broken-Skip' used - spent time refunded");
	}

	private void HandleGoldSkip()
	{
		Messenger.Notify().LogSuccess("'Gold-Skip' used");
	}

	private void HandleFreeSkip(AthCtx ctx)
	{
		ctx.AvaiableFreeSkips -= 1;
		ctx.CurrentLevel.FreeSkipped = true;
		Messenger.Notify().LogSuccess("'Free-Skip' used");
	}

	private void HandleTimeExpiredSkip()
	{
		Messenger.Notify().LogError("Well.. I tried to warn you.. Hunt is over once the level is loaded.", 10f);
	}

	private void HandlePenaltySkip()
	{
		Messenger.Notify().LogError("'Penalty-Skip' used");
	}
}