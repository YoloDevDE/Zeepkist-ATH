using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthSkippingLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		AthCtx athCtx = AthStateMachine.Ctx;

		Announce(athCtx);

		athCtx.CurrentLevel.Skipped = true;
		athCtx.CurrentLevel.Stop();
		StateMachine.TransitionTo(new StateAthLevelSummary(AthStateMachine));
	}

	/// <summary>
	///     Which kind of skip this was, in the order the run values them: a broken level costs
	///     nothing, a medal already earned costs nothing, a free skip costs one of a fixed
	///     supply, and everything after that costs time.
	/// </summary>
	private void Announce(AthCtx ctx)
	{
		if (ctx.CurrentLevel.LevelBroken)
		{
			HandleBrokenSkip(ctx);

			return;
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			HandleGoldSkip();

			return;
		}

		if (ctx.AvaiableFreeSkips > 0)
		{
			HandleFreeSkip(ctx);

			return;
		}

		if (ctx.IsTimeRunningLow)
		{
			HandleTimeExpiredSkip();

			return;
		}

		HandlePenaltySkip();
	}

	private void HandleBrokenSkip(AthCtx ctx)
	{
		ctx.CurrentLevel.LevelBroken = true;
		ToastNotification.Warn("'Broken-Skip' used - spent time refunded");
	}

	private void HandleGoldSkip()
	{
		ToastNotification.Gold("'Gold-Skip' used");
	}

	private void HandleFreeSkip(AthCtx ctx)
	{
		ctx.AvaiableFreeSkips -= 1;
		ctx.CurrentLevel.FreeSkipped = true;
		ToastNotification.Success("'Free-Skip' used");
	}

	private void HandleTimeExpiredSkip()
	{
		ToastNotification.Error("Well.. I tried to warn you.. Hunt is over once the level is loaded.", 10f);
	}

	private void HandlePenaltySkip()
	{
		ToastNotification.Error("'Penalty-Skip' used");
	}
}
