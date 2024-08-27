using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthBrokenSkip : IState
{
    public StateAthBrokenSkip(IStateMachine stateMachine)
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
        Messenger.Notify().LogWarning("'broken-Skip' used<br>Spent time refunded", 5f);
        AthStateMachine.Ctx.BrokenTimeInSeconds += (int)AthStateMachine.Ctx.CurrentLevel.Duration.TotalSeconds;

        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}