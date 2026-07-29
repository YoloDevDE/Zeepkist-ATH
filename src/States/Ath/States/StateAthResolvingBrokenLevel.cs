using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
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
        try
        {
            int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
            OnlineZeeplevel brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageBrokenLevel(brokenLevel));
        }
        catch (Exception ex)
        {
            Logger.LogWarning($"StateAthResolvingBrokenLevel: Could not send broken level message: {ex.Message}");
        }

        // Check upfront whether there is any valid level to skip to
        string currentUid = AthStateMachine.Ctx.CurrentLevel?.LevelUid;


        OnlineZeeplevel newLevel = await RandomLevelService.Instance.DrawRandomLevelAsync();
        PlaylistService.ReplaceLevelInCurrentPlaylist(new OnlineZeeplevel { UID = currentUid }, newLevel);
        PlaylistService.RestartCurrentLevel();
    }


    public override void Exit() { }

    public override void OnAthTimerTick() { }

    public override void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }
}