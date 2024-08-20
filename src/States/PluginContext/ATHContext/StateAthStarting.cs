using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        ChatApi.ClearChat();
        Messenger.SendChat(
            new Message.Builder()
                .AddLine("ATH Ranked started. gl hf!")
                .AddBreakSpace()
                .AddSeperator()
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{AthStateMachine.Ctx.Duration / 60} min")
                .AddBreakSpace()
                .AddKeyValue("Free-Skips", $"{AthStateMachine.Ctx.FreeSkips}")
                .AddBreakSpace()
                .AddKeyValue("Reward/AT", "off")
                .AddBreakSpace()
                .AddKeyValue("Punishment", $"{AthStateMachine.Ctx.PunishTime / 60} min")
                .Build()
                .ToString()
        );

        AthStateMachine.StartTimer();
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
    
    
}