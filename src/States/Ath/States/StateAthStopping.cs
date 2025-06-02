using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping(IStateMachine stateMachine) : AthState
{
    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
    }

    public override void Execute()
    {
        try
        {
            StateMachine.InvokeFinish();
            AthStateMachine.Ctx.CurrentLevel.Stop();
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageEnd());
            AthStateMachine.SetServerMessage(true);
            if (!Plugin.Instance.MyConfig.SavePlaylistOnRunEnd.Value)
            {
                return;
            }

            string playlistName = $"ATH-RUN-{DateTime.Now.ToString($"yyyy-MM-dd_HH-mm-ss_{AthStateMachine.Ctx.AuthorMedals}_{AthStateMachine.Ctx.GoldMedals}_{AthStateMachine.Ctx.Penalties}")}";
            PlaylistSaveJSON playlistSaveFile = new PlaylistSaveJSON();
            playlistSaveFile.name = playlistName;
            playlistSaveFile.levels = ZeepkistNetwork.CurrentLobby.Playlist.GetRange(0, ZeepkistNetwork.CurrentLobby.Playlist.Count - 2);
            playlistSaveFile.roundLength = 420;
            playlistSaveFile.amountOfLevels = ZeepkistNetwork.CurrentLobby.Playlist.Count - 1;
            playlistSaveFile.CreateEditor().Save();
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }


    public override void Exit()
    {
    }

    public override void OnAthTimerTick()
    {
    }
}