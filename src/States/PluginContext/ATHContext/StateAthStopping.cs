using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthStopping : IState
{
    public StateAthStopping(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        ChatApi.SendMessage("/servermessage remove");
    }
}