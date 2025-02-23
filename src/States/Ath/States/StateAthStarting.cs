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

public class StateAthStarting : IState
{
    // Constructor
    public StateAthStarting(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    public async void Execute()
    {
        // Starting Text
        ChatApi.SendMessage("/settime 86400");


        // Clear and prepare the playlist
        ZeepkistNetwork.CurrentLobby.Playlist.Clear();
        ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = 0;

        // Start the level download asynchronously
        LevelItem levelItem = await GraphQLService.Instance.GetRandomLevelAsync();
        PlaylistItem playlistItem = new PlaylistItem(
            levelItem.FileUid,
            levelItem.WorkshopId,
            levelItem.Name,
            levelItem.FileAuthor
        );
        MultiplayerApi.AddLevelToPlaylist(playlistItem, true);
        MultiplayerApi.UpdateServerPlaylist();

        AthStateMachine.Ctx.ShowMessageRunningUI();
        // Notify that the level is downloaded and start the skip coroutine
        if (Plugin.TutorialConfig.Value)
        {
            Messenger.SendChat("Level downloaded! Skipping in 7 seconds...");
        }
        else
        {
            ChatApi.SendMessage("/fs 0");
        }

        // Start the timer
        AthStateMachine.StartTimer();
    }

    public void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
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