using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthSpawning : IState
{
    // Constructor
    public StateAthSpawning(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        PhotoModeApi.PhotoModeEntered += OnRoundStarted;
        AthStateMachine.Timer.Tick += TimerOnTick;
        RacingApi.RoundEnded += OnRoundEnded;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthSkip(StateMachine));
    }

    public void Execute()
    {
        SetServerMessage();
    }

    public void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        PhotoModeApi.PhotoModeEntered -= OnRoundStarted;
        AthStateMachine.Timer.Tick -= TimerOnTick;
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    // Private Methods
    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTime += 1;
        SetServerMessage();
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage yellow 0 ATH paused  | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)}");
    }

    private void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthRunning(StateMachine));
    }
}