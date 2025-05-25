using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Level;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingNewLevel : IState
{
    private string _expectedLevelUid;

    public StateAthLoadingNewLevel(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.PlayerSpawned += OnPlayerSpawned;
        AthStateMachine.Timer.Tick += TimerOnTick;
    }

    public void Execute()
    {
        if (AthStateMachine.Ctx.CurrentLevel == null)
        {
            return;
        }

        string currentLevelStatus = AthStateMachine.Ctx.CurrentLevel.Status;
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = $"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>{currentLevelStatus}</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
        AthStateMachine.Ctx.CurrentLevel.EndTime = DateTime.Now;
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageLoadingCodex());
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTimeInSeconds += 1;
    }

    private async void OnPlayerSpawned()
    {
        // if (_expectedLevelUid != LevelApi.CurrentLevel.UID)
        // {
        //     if (AthStateMachine.Ctx.FirstLevel)
        //     {
        //         StateMachine.TransitionTo(new StateAthStarting(StateMachine));
        //         return;
        //     }
        //
        //     Messenger.Notify().LogError("Level Broken - Autoskip applied");
        //
        //     RandomLevelService.RemoveCurrentLevelFromPlaylist();
        //     await Task.Delay(2500);
        //     ChatApi.SendMessage("/fs");
        //     StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
        //     return;
        // }


        AthStateMachine.Ctx.CurrentLevel = new Level(LevelApi.CurrentLevel);


        if (AthStateMachine.Ctx.Levels.Contains(AthStateMachine.Ctx.CurrentLevel))
        {
            ChatApi.SendMessage($"/fs {ZeepkistNetwork.CurrentLobby.Playlist.Count - 1}");
            Messenger.Notify().LogError("Something went wrong.. this level should not have been loaded... skipping (dont worry no penalty is applied)");

            StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
            return;
        }

        AthStateMachine.Ctx.Levels.Add(AthStateMachine.Ctx.CurrentLevel);
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
        PlaylistService.Instance.PopulatePlaylist();
    }
}