using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
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
            if (Plugin.Instance.MyConfig.RandomPlaylist.Value)
            {
                await PlaylistService.QueueNextRandomLevel();
            }

            AthStateMachine.Ctx.ConsecutiveDuplicateCount = 0;
            AthStateMachine.Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
            AthStateMachine.SetServerMessage(true);
        }
        catch (PlaylistExhaustedException)
        {
            Logger.LogInfo("StateAthStartLevelFirstTime: All local playlist levels have been played. Stopping the run.");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
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

    public override void OnAthTimerTick() { }

    private void OnRoundStarted()
    {
        MedalTextHelper.ClearMedalText();
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}