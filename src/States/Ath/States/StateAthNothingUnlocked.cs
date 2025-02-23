using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthNothingUnlocked : IState
{
    public StateAthNothingUnlocked(IStateMachine stateMachine)
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
        Messenger.SendChat(AthStateMachine.Ctx.MessageLevelResult());
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }

    public void Exit()
    {
    }
}