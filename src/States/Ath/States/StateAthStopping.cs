using System;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping(AthStateMachine stateMachine) : AthState(stateMachine)
{
	// Properties

	public override void Enter()
	{
		try
		{
			// Null when the run is stopped before the first level was ever loaded.
			AthStateMachine.Ctx.CurrentLevel?.Stop();

			// The run report is about to take the screen; a level card behind it is noise.
			AthStateMachine.Services.LevelSummary.Hide();

			// Recorded before the report is built, so the run that just ended is the first
			// line of its own history rather than missing from it.
			AthStateMachine.Services.History.Add(BuildRecord());

			// A snapshot, taken here because everything below this state tears the run down.
			AthStateMachine.Services.Results.Show(RunReportView.From(AthStateMachine.Ctx, PlayerName(),
				AthStateMachine.Services.History.Records));
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

	/// <summary>
	///     The run, flattened into the shape that goes on disk. Built here rather than in the
	///     history service because this is the last moment the run still exists - one state
	///     further down, AthCtx is gone.
	/// </summary>
	private RunRecord BuildRecord()
	{
		AthCtx ctx = AthStateMachine.Ctx;

		return new RunRecord
		{
			EndedAt = DateTime.Now,
			PlayerName = PlayerName(),
			Gamemode = AthStateMachine.Gamemode?.DisplayName,
			LevelsPlayed = ctx.Levels.Count,
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			TotalAttempts = new RunStatistics(ctx.Levels).TotalAttempts,
			DurationMs = ctx.Duration,
			DrivenMs = ctx.GetTotalLevelPlayDuration().TotalMilliseconds,
			RanOutOfTime = ctx.IsTimeOver(),
			Levels = ctx.Levels.Select(ToRecord).ToList()
		};
	}

	private static RunLevelRecord ToRecord(Level level)
	{
		return new RunLevelRecord
		{
			Uid = level.LevelUid,
			Name = level.Name,
			Author = level.Author,
			Status = level.StatusString,
			Attempts = level.Attempt,
			Crashes = level.Crashes,
			WheelsLost = level.WheelsLost,
			DurationMs = level.GetPlayDuration().TotalMilliseconds,
			AuthorTime = level.AuthorTime,
			GoldTime = level.GoldTime,
			PersonalBest = level.PersonalBestTime
		};
	}

	/// <summary>
	///     Whoever was driving, or null when the lobby is already gone - stopping a run and
	///     leaving the server in the same breath is an ordinary thing to do.
	/// </summary>
	private static string PlayerName()
	{
		return ZeepkistNetwork.LocalPlayer?.Username;
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
