using System;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		try
		{
			AthStateMachine.Ctx.CurrentLevel?.Stop();

			AthStateMachine.Services.LevelSummary.Hide();

			AthStateMachine.Services.History.Add(BuildRecord());

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
			try
			{
				StateMachine.InvokeFinish();
			}
			catch (Exception e)
			{
				Logger.LogError(e);
			}
		}
	}

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
