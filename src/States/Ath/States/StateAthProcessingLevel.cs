using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Level;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthProcessingLevel(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }

    public override async void Execute()
    {
        try
        {
            if (IsNull(LevelApi.CurrentLevel))
            {
                await Task.Delay(100);
                StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
                return;
            }

            Level currentLevel = new Level(LevelApi.CurrentLevel);


            if (IsBrokenLevel(currentLevel))
            {
                StateMachine.TransitionTo(new StateAthResolvingBrokenLevel(StateMachine));
                return;
            }


            if (IsDuplicateLevel(currentLevel))
            {
                StateMachine.TransitionTo(new StateAthResolvingDuplicateLevel(StateMachine));
                return;
            }

            // A level we actually keep ends the duplicate streak.
            AthStateMachine.Ctx.ConsecutiveDuplicateCount = 0;
            StateMachine.TransitionTo(new StateAthStartLevelFirstTime(StateMachine));
        }
        catch (Exception ex)
        {
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            Logger.LogError($"OnPlayerSpawned: Unhandled exception: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    public override void Exit() { }

    public override void OnAthTimerTick() { }

    private bool IsBrokenLevel(Level level)
    {
        int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
        return !ZeepkistNetwork.CurrentLobby.Playlist[currentIndex].UID.Equals(level.LevelUid);
    }


    private bool IsNull(LevelScriptableObject level)
    {
        if (level != null)
        {
            return false;
        }

        Logger.LogError("OnPlayerSpawned: Failed to create Level object");
        return true;
    }

    private bool IsDuplicateLevel(Level level) => Plugin.Instance.MyConfig.RandomPlaylist.Value && AthStateMachine.Ctx.Levels.Contains(level);
}