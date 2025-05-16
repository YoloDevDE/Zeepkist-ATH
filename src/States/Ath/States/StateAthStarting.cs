using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepSDK.Chat;

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
        AthStateMachine.Ctx.FetchNextLevel();
    }

    public async void Execute()
    {
        // Starting Text
        ChatApi.SendMessage("/settime 86400");
        ZeepkistNetwork.CurrentLobby.Playlist.Clear();
        ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = 0;
        LevelItem levelItem = AthStateMachine.Ctx.NextLevel;
        MessageSenderService.SendLocalMessage(
            "Fetching 1st Level..."
        );
        // Keep fetching new level until we get a valid one
        while (levelItem == null)
        {
            await AthStateMachine.Ctx.FetchNextLevel();
            levelItem = AthStateMachine.Ctx.NextLevel;
        }

        MessageSenderService.SendLocalMessage(
            AthStateMachine.Ctx.MessageStarting()
        );
        ChatApi.SendMessage("/fs");


        AthStateMachine.StartTimer();
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}