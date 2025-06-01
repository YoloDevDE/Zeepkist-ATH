using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Level;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStartLevelFirstTime(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
    }


    public override async void Execute()
    {
        try
        {
            AthStateMachine.SetServerMessage(true);
            await PlaylistService.QueueNextRandomLevel();
            AthStateMachine.Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
        }
        catch (Exception e)
        {
            Logger.LogError($"StateAthStartLevelFirstTime: Failed to start level: {e.Message}\nStack trace: {e.StackTrace}");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
    }

    public override void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
    }

    public override void OnAthTimerTick()
    {
    }

    private void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}