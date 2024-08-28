using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPausing : IState
{
    // Constructor
    public StateAthPausing(IStateMachine stateMachine)
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
        RacingApi.RoundEnded += OnRoundEnded;
        AthStateMachine.Timer.Tick += TimerOnTick;
    }

    public void Execute()
    {
        SetServerMessage();
    }

    public void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        PhotoModeApi.PhotoModeEntered -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthEvaluateSkip(StateMachine));
    }

    // Private Methods
    private void TimerOnTick()
    {
        AthStateMachine.Ctx.PauseTimeInSeconds += 1;
        AthStateMachine.Ctx.CurrentLevel.PauseDurationInSeconds += 1;
        SetServerMessage();
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage yellow 0 ATH paused  | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)} ({TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentLevel.Duration.TotalSeconds)})");
    }

    private void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}