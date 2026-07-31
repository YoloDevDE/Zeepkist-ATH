using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingBrokenLevel(IStateMachine stateMachine) : AthState
{
    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }

    public override async void Execute()
    {
        OnlineZeeplevel brokenLevel;

        // The playlist entry at the current index is the one that failed to load - the
        // game silently fell back to another level, which is exactly how
        // StateAthProcessingLevel.IsBrokenLevel() detected the breakage. Ctx.CurrentLevel
        // is NOT this level: only StateAthStartLevelFirstTime writes into the context, so
        // on this path it still holds the previous level.
        try
        {
            int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
            brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
        }
        catch (Exception ex)
        {
            Logger.LogError($"StateAthResolvingBrokenLevel: Could not read the broken playlist entry: {ex.Message}");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        try
        {
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.Messages.BrokenLevel(brokenLevel));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"StateAthResolvingBrokenLevel: Could not send broken level message: {ex.Message}");
        }

        try
        {
            OnlineZeeplevel newLevel = await RandomLevelService.Instance.DrawRandomLevelAsync();
            PlaylistService.ReplaceLevelInCurrentPlaylist(brokenLevel, newLevel);
            PlaylistService.RestartCurrentLevel();
        }
        catch (Exception ex)
        {
            // async void - nothing above us can catch this.
            Logger.LogError($"StateAthResolvingBrokenLevel: Could not draw a replacement level: {ex.Message}");
            Messenger.Notify().LogError("Could not find a replacement for the broken level");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
    }


    public override void Exit() { }

    public override void OnAthTimerTick() { }

    public override void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }
}