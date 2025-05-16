using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;

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
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageLevelResult());
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }

    public void Exit()
    {
    }
}