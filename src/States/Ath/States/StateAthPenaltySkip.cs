using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPenaltySkip : IState
{
    public StateAthPenaltySkip(IStateMachine stateMachine)
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
        Messenger.Notify().LogError("'Penalty-Skip' used", 5f);
        AthStateMachine.Ctx.Punishments++;
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public void Exit()
    {
    }
}