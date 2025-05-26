using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthGoldSkip : IState
{
    public StateAthGoldSkip(IStateMachine stateMachine)
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
        AthStateMachine.Ctx.GoldMedals++;
        Messenger.Notify().LogCustomColors("'Gold-Skip' used", Color.black, new Color(1f, 0.84f, 0f), 5f);
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public void Exit()
    {
    }
}