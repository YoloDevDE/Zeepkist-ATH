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

    public override void Enter() { }

    public override void Execute()
    {
        try
        {
            MedalTextHelper.ClearMedalText();
            StateMachine.InvokeFinish();
            // Null when the run is stopped before the first level was ever loaded.
            AthStateMachine.Ctx.CurrentLevel?.Stop();
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageEnd());
            AthStateMachine.SetServerMessage(true);

            if (!Plugin.Instance.MyConfig.SavePlaylistOnRunEnd.Value)
            {
                return;
            }

            // The last two entries are the level that was running and the one already
            // queued behind it - neither was played, so they stay out of the saved run.
            int playedCount = Math.Max(0, ZeepkistNetwork.CurrentLobby.Playlist.Count - 2);

            if (playedCount == 0)
            {
                Logger.LogInfo("StateAthStopping: Run too short to save a playlist, skipping.");
                return;
            }

            string playlistName = $"ATH-RUN-{DateTime.Now.ToString($"yyyy-MM-dd_HH-mm-ss_{AthStateMachine.Ctx.AuthorMedals}_{AthStateMachine.Ctx.GoldMedals}_{AthStateMachine.Ctx.Penalties}")}";
            PlaylistSaveJSON playlistSaveFile = new PlaylistSaveJSON();
            playlistSaveFile.name = playlistName;
            playlistSaveFile.levels = ZeepkistNetwork.CurrentLobby.Playlist.GetRange(0, playedCount);
            playlistSaveFile.roundLength = 420;
            playlistSaveFile.amountOfLevels = playedCount;
            playlistSaveFile.CreateEditor().Save();
        }
        catch (Exception e)
        {
            Logger.LogError(e);
        }
    }


    public override void Exit() { }

    public override void OnAthTimerTick() { }
}