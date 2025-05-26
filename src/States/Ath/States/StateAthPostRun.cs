using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPostRun : IState
{
    public StateAthPostRun(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.PlayerSpawned += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
        AthStateMachine.Timer.Tick += TimerOnTick;
    }

    public void Execute()
    {
        AthStateMachine.SetServerMessage(true);
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.PauseTimeInSeconds += 1;
        AthStateMachine.Ctx.CurrentLevel.PauseDurationInSeconds += 1;
        AthStateMachine.SetServerMessage(true);
    }

    // Private Methods
    private void OnRoundStarted()
    {
        ChatApi.SendMessage("/fs");
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }
}