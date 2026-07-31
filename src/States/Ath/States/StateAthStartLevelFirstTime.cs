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


    public override void Execute()
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

    /// <summary>
    ///     Pre-loads the level after the current one so the playlist never runs dry. Started
    ///     without awaiting - the run continues either way - so it has to swallow and report
    ///     its own failures. Unhandled, they would end up in an unobserved Task and the
    ///     playlist would simply be empty at the end with nothing in the log to explain it.
    /// </summary>
    private async Task AddLevelAsync()
    {
        if (!Plugin.Instance.MyConfig.RandomPlaylist.Value)
        {
            return;
        }

        try
        {
            Logger.LogInfo("Adding Random Level");
            OnlineZeeplevel level = await RandomLevelService.Instance.DrawRandomLevelAsync();
            PlaylistService.AddLevelToCurrentPlaylist(level);
        }
        catch (Exception e)
        {
            Logger.LogError($"StateAthStartLevelFirstTime: Could not pre-load the next level: {e.Message}");
            Messenger.Notify().LogWarning("Could not load the next level - the playlist may run out");
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