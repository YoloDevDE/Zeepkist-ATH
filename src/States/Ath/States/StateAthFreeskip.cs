using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Chat;

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
        ChatApi.SendMessage("Freeskip");
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}