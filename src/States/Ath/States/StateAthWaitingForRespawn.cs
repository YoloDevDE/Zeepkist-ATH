using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn : AthState
{
    public StateAthWaitingForRespawn(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
        AthStateMachine.Ctx.CurrentLevel.StartPause();
    }

    public override void Execute()
    {
        AthStateMachine.SetServerMessage(true);
    }

    public override void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        AthStateMachine.Ctx.CurrentLevel.EndPause();
    }

    public override void OnAthTimerTick()
    {
        AthStateMachine.Ctx.PauseTimeInSeconds += 1;
        AthStateMachine.Ctx.CurrentLevel.UpdatePause();
        AthStateMachine.SetServerMessage(true);
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }


    // Private Methods
    private void OnRoundStarted()
    {
        ChatApi.SendMessage("/fs");
    }
}