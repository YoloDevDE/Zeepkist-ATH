using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Level;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingNewLevel : IState
{
    public StateAthLoadingNewLevel(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        AthStateMachine.Timer.Tick += TimerOnTick;
    }

    public async void Execute()
    {
        LevelItem levelItem;

        // Fetch a random level for PreCachedLevel
        AthStateMachine.Ctx.PreCachedLevel = await GraphQLService.Instance.GetRandomLevelAsync();

        if (AthStateMachine.Ctx.PreCachedLevel == null)
        {
            // If PreCachedLevel is null, get a random level for both levelItem and PreCachedLevel
            levelItem = await GraphQLService.Instance.GetRandomLevelAsync();
        }
        else
        {
            // If PreCachedLevel is not null, use it as the current level and fetch a new random level for PreCachedLevel
            levelItem = AthStateMachine.Ctx.PreCachedLevel;
        }

        AthStateMachine.Ctx.PreCachedLevel = await GraphQLService.Instance.GetRandomLevelAsync();

        // Create a playlist item from the current levelItem and add it to the playlist
        PlaylistItem playlistItem = new PlaylistItem(
            levelItem.FileUid,
            levelItem.WorkshopId,
            levelItem.Name,
            levelItem.FileAuthor
        );
        MultiplayerApi.AddLevelToPlaylist(playlistItem, true);
        MultiplayerApi.UpdateServerPlaylist();

        // Continue with the synchronous part if CurrentLevel is set
        if (AthStateMachine.Ctx.CurrentLevel == null)
        {
            return;
        }

        // Update CurrentLevel end time and send a loading message
        AthStateMachine.Ctx.CurrentLevel.EndTime = DateTime.Now;
        Messenger.SendChat(AthStateMachine.Ctx.MessageLoadingCodex());
    }

    public void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        AthStateMachine.Timer.Tick -= TimerOnTick;
    }

    private void TimerOnTick()
    {
        AthStateMachine.Ctx.LoadingTimeInSeconds += 1;
    }

    private void OnLevelLoaded()
    {
        MultiplayerApi.SetNextLevelIndex(ZeepkistNetwork.CurrentLobby.Playlist.Count - 1);
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
    }
}