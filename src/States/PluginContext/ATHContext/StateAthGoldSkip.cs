using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        ChatApi.SendMessage("Goldskip");
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
}