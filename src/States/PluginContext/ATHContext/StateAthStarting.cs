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
        AthStateMachine.Timer.Tick += TimerOnTick;
        RacingApi.PlayerSpawned += OnPlayerSpawned;
    }

    private void OnPlayerSpawned()
    {
        StateMachine.TransitionTo(new StateAthSpawning(StateMachine));
    }

    public void Execute()
    {
        // Starting Text
        ChatApi.SendMessage("/settime 86400");
        ChatApi.SendMessage("/fs");
        Messenger.SendChat(
            new Message.Builder()
                .AddLine("ATH started. gl hf!")
                .AddBreakSpace()
                .AddSeperator()
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{AthStateMachine.Ctx.Duration / 60} min")
                .AddBreakSpace()
                .AddKeyValue("Free-Skips", $"{AthStateMachine.Ctx.FreeSkips}")
                .AddBreakSpace()
                .AddKeyValue("Punishment", $"{AthStateMachine.Ctx.PunishTime / 60} min")
                .Build()
                .ToString()
        );

        AthStateMachine.StartTimer();
    }

    public void Exit()
    {
        AthStateMachine.Timer.Tick -= TimerOnTick;
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
    }

    // Private Methods
    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTime += 1;
    }
}