using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthSkip : IState
{    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;
    public StateAthSkip(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        ChatApi.SendMessage("...Checking if:<br> - time ran out<br> - forceskip was used<br> - other");
        AthStateMachine.Ctx.Punishments++;
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
}