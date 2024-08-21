using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        ChatApi.SendMessage(AthStateMachine.Ctx.MessageFinish());
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
    }

    public void Exit()
    {
    }
}