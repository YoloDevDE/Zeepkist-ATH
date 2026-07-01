using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingBrokenLevel(IStateMachine stateMachine) : AthState
{
    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

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

        if (!PlaylistService.HasValidNextLevel(currentUid))
        {
            Logger.LogWarning("StateAthResolvingBrokenLevel: No valid next level available (playlist exhausted). Ending run.");
            Messenger.Notify().LogCustomColors("RandomLevelService warning:<br>Could not fetch a new unique level.<br>Run will stop after this map.", Color.white, Color.red, 8f);
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        await PlaylistService.QueueNextRandomLevel();
        PlaylistService.SkipToLastLevel();
    }


    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    public override void OnAthTimerTick() { }

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }
}