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

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingNewLevel : IState
{
    #region Constructor

    public StateAthLoadingNewLevel(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    #endregion

    #region Properties & Fields

    public IStateMachine StateMachine { get; }
    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    #endregion

    #region IState Implementation

    public void Enter()
    {
        AthStateMachine.Timer.Tick += TimerOnTick;
    }


    public async void Execute()
    {
        try
        {
            Logger.LogDebug($"OnPlayerSpawned: Current level UID: {LevelApi.CurrentLevel?.UID}");

            if (!ValidateCurrentLevel())
            {
                return;
            }

            if (IsBrokenLevel())
            {
                await HandleBrokenLevel();
                return;
            }

            if (!CreateAndValidateNewLevel())
            {
                return;
            }

            if (IsDuplicateLevel())
            {
                await HandleDuplicateLevel();
                return;
            }

            await ProcessValidLevel();
        }
        catch (Exception ex)
        {
            Logger.LogError($"OnPlayerSpawned: Unhandled exception: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }

    public void Exit()
    {
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    #endregion

    #region Private Methods

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTimeInSeconds += 1;
    }


    private bool ValidateCurrentLevel()
    {
        if (LevelApi.CurrentLevel != null)
        {
            return true;
        }

        Logger.LogError("OnPlayerSpawned: Current level is null");
        return false;
    }

    private bool IsBrokenLevel()
    {
        return !ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex].UID.Equals(LevelApi.CurrentLevel.UID);
    }

    private async Task HandleBrokenLevel()
    {
        Logger.LogWarning($"OnPlayerSpawned: Level {LevelApi.CurrentLevel.UID} appears to be broken");
        Messenger.Notify().LogError($"Level Broken - Autoskip applied<br>{ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex].UID}");
        await PlaylistService.Instance.ReplaceBrokenLevel();
        await Task.Delay(500);
        ChatApi.SendMessage("/fs");
        Logger.LogInfo("OnPlayerSpawned: Transitioning to new LoadingNewLevel state after broken level");
        StateMachine.TransitionTo(new StateAthLoadingThroughBrokenLevel(StateMachine));
    }

    private bool CreateAndValidateNewLevel()
    {
        Logger.LogDebug("OnPlayerSpawned: Creating new Level object");
        AthStateMachine.Ctx.CurrentLevel = new Level(LevelApi.CurrentLevel);

        if (AthStateMachine.Ctx.CurrentLevel != null)
        {
            return true;
        }

        Logger.LogError("OnPlayerSpawned: Failed to create Level object");
        return false;
    }

    private bool IsDuplicateLevel()
    {
        return AthStateMachine.Ctx.Levels.Contains(AthStateMachine.Ctx.CurrentLevel);
    }

    private async Task HandleDuplicateLevel()
    {
        if (ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex == ZeepkistNetwork.CurrentLobby.Playlist.Count - 1)
        {
            await PlaylistService.Instance.PopulatePlaylist();
        }

        Logger.LogWarning("OnPlayerSpawned: Duplicate level detected");
        ChatApi.SendMessage($"/fs {ZeepkistNetwork.CurrentLobby.Playlist.Count - 1}");
        Messenger.Notify().LogError("Something went wrong.. this level should not have been loaded... skipping (dont worry no penalty is applied)");
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    private async Task ProcessValidLevel()
    {
        Logger.LogDebug("OnPlayerSpawned: Adding level to tracked levels");
        AthStateMachine.Ctx.Levels.Add(AthStateMachine.Ctx.CurrentLevel);
        Logger.LogInfo("OnPlayerSpawned: Transitioning to Pausing state");
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
        await PlaylistService.Instance.PopulatePlaylist();
    }

    #endregion
}