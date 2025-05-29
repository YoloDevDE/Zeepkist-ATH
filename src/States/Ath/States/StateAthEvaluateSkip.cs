using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthEvaluateSkip : AthState
{
    public StateAthEvaluateSkip(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
    }

    public override void Execute()
    {
        AthCtx athCtx = AthStateMachine.Ctx;
        athCtx.CurrentLevel.LevelSkipped = true;

        if (athCtx.CurrentLevel.LevelBroken)
        {
            HandleBrokenSkip(athCtx);
        }
        else
        {
            athCtx.Skips += 1;

            if (athCtx.CurrentLevel.GoldSkipUnlocked)
            {
                HandleGoldSkip(athCtx);
            }
            else if (athCtx.FreeSkips > 0)
            {
                HandleFreeSkip(athCtx);
            }
            else if (athCtx.EndTime <= DateTime.Now.AddSeconds(athCtx.PunishTime))
            {
                HandleTimeExpiredSkip(athCtx);
            }
            else
            {
                HandlePenaltySkip(athCtx);
            }
        }

        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public override void Exit()
    {
    }

    public override void OnAthTimerTick()
    {
    }

    private static void HandleBrokenSkip(AthCtx ctx)
    {
        Messenger.Notify().LogWarning("'Broken-Skip' used<br>Spent time refunded", 5f);
        ctx.BrokenTimeInSeconds += (int)ctx.CurrentLevel.PlayDuration.TotalSeconds;
    }

    private static void HandleGoldSkip(AthCtx ctx)
    {
        ctx.GoldMedals++;
        Messenger.Notify().LogCustomColors("'Gold-Skip' used", Color.black, new Color(1f, 0.84f, 0f), 5f);
    }

    private static void HandleFreeSkip(AthCtx ctx)
    {
        ctx.FreeSkips--;
        ctx.CurrentLevel.FreeSkipped = true;
        Messenger.Notify().LogCustomColors("'Free-Skip' used", Color.black, Color.white, 5f);
    }

    private static void HandleTimeExpiredSkip(AthCtx ctx)
    {
        Messenger.Notify().LogCustomColors("Well.. I tried to warn you.. Challenge is over once the level is loaded.", Color.white, Color.red, 10f);
        ctx.Punishments++;
    }

    private static void HandlePenaltySkip(AthCtx ctx)
    {
        Messenger.Notify().LogError("'Penalty-Skip' used", 5f);
        ctx.Punishments++;
    }
}