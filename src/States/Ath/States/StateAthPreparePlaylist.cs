using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPreparePlaylist : IState
{
    // Constructor
    public StateAthPreparePlaylist(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
    }

    public async void Execute()
    {
        await PlaylistService.Instance.WaitForPlaylistReady();
        ZeepkistNetwork.CurrentLobby.Playlist.Clear();
        ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = 0;
        ZeepkistNetwork.CurrentLobby.NextPlaylistIndex = 1;
        RandomLevelService.NextLevelProcedure();
        await PlaylistService.Instance.WaitForPlaylistReady();

        ChatApi.SendMessage("/fs 0");
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}