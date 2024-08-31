using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping : IState
{
    public StateAthStopping(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        AthStateMachine.StopTimer();
        try
        {
            ChatApi.SendMessage(AthStateMachine.Ctx.MessageFinalResult());
            ChatApi.SendMessage("/servermessage blue 0 ATH finished!");
            if (Plugin.SavePlaylistOnRunEnd.Value)
            {
                string playlistName = $"ATH-RUN-{DateTime.Now.ToString($"yyyy-MM-dd_HH-mm-ss_{AthStateMachine.Ctx.AuthorMedals}_{AthStateMachine.Ctx.GoldMedals}_{AthStateMachine.Ctx.Skips}")}";
                PlaylistSaveJSON playlistSaveFile = new PlaylistSaveJSON();
                playlistSaveFile.name = playlistName;
                playlistSaveFile.levels = ZeepkistNetwork.CurrentLobby.Playlist;
                playlistSaveFile.roundLength = 420;
                playlistSaveFile.amountOfLevels = ZeepkistNetwork.CurrentLobby.Playlist.Count;
                playlistSaveFile.CreateEditor().Save();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public void Exit()
    {
    }
}