using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthStopping : IState
{
    public StateAthStopping(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        ChatApi.SendMessage("Finished you worm");
        AthStateMachine.StopTimer();
    }

    public void Exit()
    {
        ChatApi.SendMessage("/servermessage remove");
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = true;
    }
}