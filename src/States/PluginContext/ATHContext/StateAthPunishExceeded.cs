using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthPunishExceeded : IState
{
    public StateAthPunishExceeded(IStateMachine stateMachine)
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
        ChatApi.SendMessage("PunishExceeded");
        StateMachine.TransitionTo(StateMachine.LastState);
    }

    public void Exit()
    {
    }
}