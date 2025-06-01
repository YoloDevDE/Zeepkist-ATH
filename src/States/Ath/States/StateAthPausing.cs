using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPausing(IStateMachine stateMachine) : AthState
{
    // Constructor


    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    // Public Methods
    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        PhotoModeApi.PhotoModeEntered += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
    }

    public override void Execute()
    {
        AthStateMachine.Ctx.ResetRetries();
        AthStateMachine.SetServerMessage(true);
    }

    public override void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        PhotoModeApi.PhotoModeEntered -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthEvaluateSkip(StateMachine));
    }

    // Private Methods
    public override void OnAthTimerTick()
    {
        AthStateMachine.SetServerMessage(true);
    }


    private void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}