using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Level;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingThroughBrokenLevel : AthState
{
    public StateAthLoadingThroughBrokenLevel(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    private PlaylistService PlaylistService => PlaylistService.Instance;

    // Properties
    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    public override void Execute()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = "<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>Broken</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageBrokenLevel(PlaylistService.CurrentBrokenZeeplevel));
        AthStateMachine.Ctx.Retries--;
        if (AthStateMachine.Ctx.Retries <= 0)
        {
            Logger.LogWarning("OnRoundStarted: Retries exceeded");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
    }


    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    public override void OnAthTimerTick()
    {
    }

    private async void OnLevelLoaded()
    {
        try
        {
            Logger.LogDebug($"OnRoundStarted: Current level UID: {LevelApi.CurrentLevel?.UID}");

            if (LevelApi.CurrentLevel == null)
            {
                Logger.LogError("OnRoundStarted: Current level is null");
                return;
            }

            if (!ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex].UID.Equals(LevelApi.CurrentLevel.UID))
            {
                Logger.LogWarning($"OnRoundStarted: Level {PlaylistService.CurrentBrokenZeeplevel.UID} appears to be broken");
                Messenger.Notify().LogError($"Level Broken - Autoskip applied<br>{PlaylistService.CurrentBrokenZeeplevel.UID}");
                await PlaylistService.ReplaceBrokenLevel();
                await Task.Delay(500);
                PlaylistService.SkipLevel();
                Logger.LogInfo("OnRoundStarted: Transitioning to new LoadingNewLevel state after broken level");
                StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));

                return;
            }

            Logger.LogDebug("OnRoundStarted: Creating new Level object");
            AthStateMachine.Ctx.CurrentLevel = new Level(LevelApi.CurrentLevel);

            if (AthStateMachine.Ctx.CurrentLevel == null)
            {
                Logger.LogError("OnRoundStarted: Failed to create Level object");
                return;
            }

            if (AthStateMachine.Ctx.Levels.Contains(AthStateMachine.Ctx.CurrentLevel))
            {
                if (ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex == ZeepkistNetwork.CurrentLobby.Playlist.Count - 1)
                {
                    await PlaylistService.PopulatePlaylist();
                }

                Logger.LogWarning("OnRoundStarted: Duplicate level detected");
                PlaylistService.SkipToLastLevel();
                Messenger.Notify().LogError("Something went wrong.. this level should not have been loaded... skipping (dont worry no penalty is applied)");
                StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
                return;
            }

            Logger.LogDebug("OnRoundStarted: Adding level to tracked levels");
            AthStateMachine.Ctx.Levels.Add(AthStateMachine.Ctx.CurrentLevel);
            Logger.LogInfo("OnRoundStarted: Transitioning to Pausing state");
            StateMachine.TransitionTo(new StateAthPausing(StateMachine));
            await PlaylistService.PopulatePlaylist();
        }
        catch (Exception ex)
        {
            Logger.LogError($"OnRoundStarted: Unhandled exception: {ex.Message}\nStack trace: {ex.StackTrace}");
        }
    }
}