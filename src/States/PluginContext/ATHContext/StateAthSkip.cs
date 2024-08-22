using System;
using AuthorTimeHunting.Interfaces;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthSkip : IState
{
    public StateAthSkip(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        AthStateMachine.Ctx.CurrentLevel.LevelSkipped = true;
        if (AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked)
        {
            StateMachine.TransitionTo(new StateAthGoldSkip(StateMachine));
            return;
        }

        if (AthStateMachine.Ctx.FreeSkips > 0)
        {
            StateMachine.TransitionTo(new StateAthFreeskip(StateMachine));
            return;
        }

        if (AthStateMachine.Ctx.EndTime <= DateTime.Now.AddSeconds(AthStateMachine.Ctx.PunishTime))
        {
            StateMachine.TransitionTo(new StateAthPunishExceeded(StateMachine));
            return;
        }


        StateMachine.TransitionTo(new StateAthPunishSkip(StateMachine));
    }

    public void Exit()
    {
    }
}