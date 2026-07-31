using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping(AthStateMachine stateMachine) : AthState(stateMachine)
{
	// Properties

	public override void Execute()
	{
		try
		{
			Overlay.ClearBanner();
			// Null when the run is stopped before the first level was ever loaded.
			AthStateMachine.Ctx.CurrentLevel?.Stop();
			AthStateMachine.Show(AthStateMachine.Ctx.Messages.End());
			AthStateMachine.SetServerMessage(true);
			SavePlaylistIfConfigured();
		}
		catch (Exception e)
		{
			Logger.LogError(e);
		}
		finally
		{
			// Last, not second. InvokeFinish hands control to StateMasterOn.Stop, which
			// tears this state machine down and destroys its GameObject - everything
			// above has to have happened by then. In the finally block so that a failed
			// end summary still shuts the mod down instead of leaving it half-running.
			try
			{
				StateMachine.InvokeFinish();
			}
			catch (Exception e)
			{
				// We are inside a chat command handler; letting this escape helps nobody.
				Logger.LogError(e);
			}
		}
	}

	private void SavePlaylistIfConfigured()
	{
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

		AthCtx ctx = AthStateMachine.Ctx;
		string playlistName =
			$"ATH-RUN-{DateTime.Now.ToString($"yyyy-MM-dd_HH-mm-ss_{ctx.AuthorMedals}_{ctx.GoldMedals}_{ctx.Penalties}")}";
		PlaylistSaveJSON playlistSaveFile = new();
		playlistSaveFile.name = playlistName;
		playlistSaveFile.levels = ZeepkistNetwork.CurrentLobby.Playlist.GetRange(0, playedCount);
		playlistSaveFile.roundLength = 420;
		playlistSaveFile.amountOfLevels = playedCount;
		playlistSaveFile.CreateEditor().Save();
	}
}