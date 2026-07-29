using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using ZeepSDK.Level;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStartLevelFirstTime(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }


    public override async void Execute()
    {
        try
        {
            Logger.LogInfo($"Plugin.Instance.MyConfig.RandomPlaylist.Value : {Plugin.Instance.MyConfig.RandomPlaylist.Value}");

            AthStateMachine.Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
            AthStateMachine.SetServerMessage(true);
        }
        catch (Exception e)
        {
            Logger.LogError($"StateAthStartLevelFirstTime: Failed to start level: {e.Message}\nStack trace: {e.StackTrace}");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
    }

    private async Task AddLevelAsync()
    {
        if (Plugin.Instance.MyConfig.RandomPlaylist.Value)
        {
            Logger.LogInfo("Adding Random Level");
            OnlineZeeplevel level = await RandomLevelService.Instance.DrawRandomLevelAsync();
            PlaylistService.AddLevelToCurrentPlaylist(level);
        }
    }


    public override void Exit() { }

    public override void OnAthTimerTick() { }

    public override void OnRoundStarted()
    {
        _ = AddLevelAsync();
        MedalTextHelper.ClearMedalText();
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }
}