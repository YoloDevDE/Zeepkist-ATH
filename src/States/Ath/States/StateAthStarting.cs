using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStarting : IState
{
    // Constructor
    public StateAthStarting(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
    }

    public void Execute()
    {
        // Starting Text
        ChatApi.SendMessage("/settime 86400");
        ChatApi.SendMessage("/fs");
        Messenger.SendChat(
            AthStateMachine.Ctx.MessageStarting()
        );

        AthStateMachine.StartTimer();
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}