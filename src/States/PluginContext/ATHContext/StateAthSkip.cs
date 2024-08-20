using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthSkip : IState
{
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
        ChatApi.SendMessage("...Checking which if:<br> - time ran out<br> - forceskip was used<br> - other");
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
}