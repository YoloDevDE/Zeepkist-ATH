using System;
using System.Threading.Tasks;
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

public class StateAthLoadingThroughBrokenLevel : IState
{
    public StateAthLoadingThroughBrokenLevel(IStateMachine stateMachine)
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
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = "<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>Broken</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageBrokenLevel(PlaylistService.Instance.CurrentBrokenZeeplevel));
        AthStateMachine.Ctx.Retries--;
        if (AthStateMachine.Ctx.Retries <= 0)
        {
            Logger.LogWarning("OnPlayerSpawned: Retries exceeded");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
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
        try
        {
            Logger.LogDebug($"OnPlayerSpawned: Current level UID: {LevelApi.CurrentLevel?.UID}");

            if (LevelApi.CurrentLevel == null)
            {
                Logger.LogError("OnPlayerSpawned: Current level is null");
                return;
            }

            if (!ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex].UID.Equals(LevelApi.CurrentLevel.UID))
            {
                Logger.LogWarning($"OnPlayerSpawned: Level {PlaylistService.Instance.CurrentBrokenZeeplevel.UID} appears to be broken");
                Messenger.Notify().LogError($"Level Broken - Autoskip applied<br>{PlaylistService.Instance.CurrentBrokenZeeplevel.UID}");
                await PlaylistService.Instance.ReplaceBrokenLevel();
                await Task.Delay(500);
                ChatApi.SendMessage("/fs");
                Logger.LogInfo("OnPlayerSpawned: Transitioning to new LoadingNewLevel state after broken level");
                StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));

                return;
            }

            Logger.LogDebug("OnPlayerSpawned: Creating new Level object");
            AthStateMachine.Ctx.CurrentLevel = new Level(LevelApi.CurrentLevel);

            if (AthStateMachine.Ctx.CurrentLevel == null)
            {
                Logger.LogError("OnPlayerSpawned: Failed to create Level object");
                return;
            }

            if (AthStateMachine.Ctx.Levels.Contains(AthStateMachine.Ctx.CurrentLevel))
            {
                if (ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex == ZeepkistNetwork.CurrentLobby.Playlist.Count - 1)
                {
                    await PlaylistService.Instance.PopulatePlaylist();
                }

                Logger.LogWarning("OnPlayerSpawned: Duplicate level detected");
                ChatApi.SendMessage($"/fs {ZeepkistNetwork.CurrentLobby.Playlist.Count - 1}");
                Messenger.Notify().LogError("Something went wrong.. this level should not have been loaded... skipping (dont worry no penalty is applied)");
                StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
                return;
            }

            Logger.LogDebug("OnPlayerSpawned: Adding level to tracked levels");
            AthStateMachine.Ctx.Levels.Add(AthStateMachine.Ctx.CurrentLevel);
            Logger.LogInfo("OnPlayerSpawned: Transitioning to Pausing state");
            StateMachine.TransitionTo(new StateAthPausing(StateMachine));
            await PlaylistService.Instance.PopulatePlaylist();
        }
        catch (Exception ex)
        {
            Logger.LogError($"OnPlayerSpawned: Unhandled exception: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }
}