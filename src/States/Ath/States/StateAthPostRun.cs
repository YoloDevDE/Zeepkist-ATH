using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
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
        SetServerMessage();
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.PauseTimeInSeconds += 1;
        AthStateMachine.Ctx.CurrentLevel.PauseDurationInSeconds += 1;
        SetServerMessage();
    }

    // Private Methods
    private void OnRoundStarted()
    {
        ChatApi.SendMessage("/fs");
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage yellow 0 ATH paused | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)}");
    }
}