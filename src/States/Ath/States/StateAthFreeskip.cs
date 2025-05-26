using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthFreeskip : IState
{
    public StateAthFreeskip(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        AthStateMachine.Ctx.FreeSkips--;

        AthStateMachine.Ctx.CurrentLevel.FreeSkipped = true;
    }

    public void Execute()
    {
        Messenger.Notify().LogCustomColors("'Free-Skip' used", Color.black, Color.white, 5f);
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public void Exit()
    {
    }
}