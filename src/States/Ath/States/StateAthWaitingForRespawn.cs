using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn : AthState
{
    public StateAthWaitingForRespawn(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    private PlaylistService PlaylistService => PlaylistService.Instance;

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
        AthStateMachine.Ctx.CurrentLevel.Stop();
    }

    public override void Execute()
    {
        AthStateMachine.SetServerMessage(true);
    }

    public override void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    public override void OnAthTimerTick()
    {
        AthStateMachine.SetServerMessage(true);
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }


    // Private Methods
    private void OnRoundStarted()
    {
        PlaylistService.SkipLevel();
    }
}