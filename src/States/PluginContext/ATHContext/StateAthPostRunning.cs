using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthPostRunning : IState
{
    // Constructor
    public StateAthPostRunning(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        RacingApi.PlayerSpawned += OnRoundStarted;
    }

    public void Execute()
    {
        SetServerMessage();
        AthStateMachine.Ctx.AuthorMedals++;
    }
    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnRoundStarted;
    }

    // Private Methods
    private void OnRoundStarted()
    {
        ChatApi.SendMessage("/fs");
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage yellow 0 ATH paused | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)}");
    }
}