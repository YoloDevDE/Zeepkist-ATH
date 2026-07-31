using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

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

	private static void HandleBrokenSkip(AthCtx ctx)
	{
		ctx.CurrentLevel.LevelBroken = true;
		Messenger.Notify().LogWarning("'Broken-Skip' used<br>Spent time refunded", 5f);
	}

	private static void HandleGoldSkip()
	{
		Messenger.Notify().LogCustomColors("'Gold-Skip' used", Color.black, new Color(1f, 0.84f, 0f), 5f);
	}

	private static void HandleFreeSkip(AthCtx ctx)
	{
		ctx.AvaiableFreeSkips -= 1;
		ctx.CurrentLevel.FreeSkipped = true;
		Messenger.Notify().LogCustomColors("'Free-Skip' used", Color.black, Color.white, 5f);
	}

	private static void HandleTimeExpiredSkip()
	{
		Messenger.Notify().LogCustomColors("Well.. I tried to warn you.. Hunt is over once the level is loaded.",
			Color.white, Color.red, 10f);
	}

	private static void HandlePenaltySkip()
	{
		Messenger.Notify().LogError("'Penalty-Skip' used", 5f);
	}
}