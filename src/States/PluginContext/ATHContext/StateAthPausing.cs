using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        StateMachine.TransitionTo(new StateAthSkip(StateMachine));
    }

    // Private Methods
    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTimeInSeconds += 1;
        AthStateMachine.Ctx.CurrentLevel.PauseDurationInSeconds += 1;
        SetServerMessage();
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage yellow 0 ATH paused  | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)}");
    }

    private void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}