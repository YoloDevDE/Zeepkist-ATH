using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthEvaluateSkip(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }

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
                HandleGoldSkip(athCtx);
            }
            else if (athCtx.AvaiableFreeSkips > 0)
            {
                HandleFreeSkip(athCtx);
            }
            else if (athCtx.IsTimeRunningLow)
            {
                HandleTimeExpiredSkip(athCtx);
            }
            else
            {
                HandlePenaltySkip(athCtx);
            }
        }

        athCtx.CurrentLevel.Skipped = true;
        athCtx.CurrentLevel.Stop();
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public override void Exit() { }

    public override void OnAthTimerTick() { }

    private static void HandleBrokenSkip(AthCtx ctx)
    {
        ctx.CurrentLevel.LevelBroken = true;
        Messenger.Notify().LogWarning("'Broken-Skip' used<br>Spent time refunded", 5f);
    }

    private static void HandleGoldSkip(AthCtx ctx)
    {
        Messenger.Notify().LogCustomColors("'Gold-Skip' used", Color.black, new Color(1f, 0.84f, 0f), 5f);
    }

    private static void HandleFreeSkip(AthCtx ctx)
    {
        ctx.AvaiableFreeSkips -= 1;
        ctx.CurrentLevel.FreeSkipped = true;
        Messenger.Notify().LogCustomColors("'Free-Skip' used", Color.black, Color.white, 5f);
    }

    private static void HandleTimeExpiredSkip(AthCtx ctx)
    {
        Messenger.Notify().LogCustomColors("Well.. I tried to warn you.. Hunt is over once the level is loaded.", Color.white, Color.red, 10f);
    }

    private static void HandlePenaltySkip(AthCtx ctx)
    {
        Messenger.Notify().LogError("'Penalty-Skip' used", 5f);
    }
}