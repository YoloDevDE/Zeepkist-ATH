using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
}