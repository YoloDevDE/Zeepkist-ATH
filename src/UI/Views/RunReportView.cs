using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     The end-of-run report, as data.
///     A snapshot, deliberately: it is built the moment a run stops and the run's state
///     machine is torn down immediately afterwards. A view that held on to AthCtx would be
///     reading a corpse by the time the player got round to looking at it.
/// </summary>
public class RunReportView
{
	private RunReportView()
	{
	}

	public string Kicker { get; private set; }

	public string Headline { get; private set; }

	public string PlayerName { get; private set; }

	public IReadOnlyList<HistoryRow> History { get; private set; }

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	public IReadOnlyList<ReportRow> Summary { get; private set; }

	public IReadOnlyList<ReportRow> Records { get; private set; }

	public IReadOnlyList<LevelRow> Levels { get; private set; }

	public static RunReportView From(AthCtx ctx)
	{
		return From(ctx, null, null);
	}

	public static RunReportView From(AthCtx ctx, string playerName, IReadOnlyList<RunRecord> history)
	{
		if (ctx == null)
		{
			return null;
		}

		RunStatistics stats = new(ctx.Levels);
		TimeSpan spent = ctx.GetTotalLevelPlayDuration();

		return new RunReportView
		{
			Kicker = "RUN OVER",
			Headline = Headlines(ctx),
			PlayerName = Name(playerName),
			History = BuildHistory(history),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			Summary =
			[
				new ReportRow("Levels Played", ctx.Levels.Count.ToString()),
				new ReportRow("Author Times", ctx.AuthorMedals.ToString(), Color.Zeepkist.Medal.Author),
				new ReportRow("Gold Skips", ctx.GoldMedals.ToString(), Color.Zeepkist.Medal.Gold),
				new ReportRow("Penalty Skips", ctx.Penalties.ToString(), Color.Style.Status.Penalty),
				new ReportRow("Time Budget", TimeSpan.FromMilliseconds(ctx.Duration).ToFormattedString()),
				new ReportRow("Time Driven", spent.ToFormattedString()),
				new ReportRow("Time Left",
					TimeFormatter.FormatDuration((int)ctx.GetRemainingTime().TotalMilliseconds)),
				new ReportRow("Total Attempts", stats.TotalAttempts.ToString())
			],
			Records = BuildRecords(stats),
			Levels = BuildLevels(ctx.Levels)
		};
	}

	public static RunReportView HistoryOnly(string playerName, IReadOnlyList<RunRecord> history)
	{
		IReadOnlyList<RunRecord> records = history ?? [];

		return new RunReportView
		{
			Kicker = "MATCH HISTORY",
			Headline = records.Count == 0 ? "No runs recorded yet" :
				records.Count == 1 ? "1 run on record" : $"{records.Count} runs on record",
			PlayerName = Name(playerName),
			History = BuildHistory(records),
			AuthorMedals = records.Sum(record => record.AuthorMedals),
			GoldMedals = records.Sum(record => record.GoldMedals),
			Penalties = records.Sum(record => record.Penalties),
			Summary = [],
			Records = [],
			Levels = []
		};
	}

	public static RunReportView FromRecord(RunRecord record)
	{
		if (record == null)
		{
			return null;
		}

		IReadOnlyList<RunLevelRecord> levels = record.Levels ?? [];

		return new RunReportView
		{
			Kicker = record.EndedAt.ToString("yyyy-MM-dd HH:mm"),
			Headline = record.AuthorMedals == 1 ? "1 author time" : $"{record.AuthorMedals} author times",
			PlayerName = Name(record.PlayerName),
			History = [],
			AuthorMedals = record.AuthorMedals,
			GoldMedals = record.GoldMedals,
			Penalties = record.Penalties,
			Summary =
			[
				new ReportRow("Levels Played", record.LevelsPlayed.ToString()),
				new ReportRow("Author Times", record.AuthorMedals.ToString(), Color.Zeepkist.Medal.Author),
				new ReportRow("Gold Skips", record.GoldMedals.ToString(), Color.Zeepkist.Medal.Gold),
				new ReportRow("Penalty Skips", record.Penalties.ToString(), Color.Style.Status.Penalty),
				new ReportRow("Time Budget", TimeSpan.FromMilliseconds(record.DurationMs).ToFormattedString()),
				new ReportRow("Time Driven", TimeSpan.FromMilliseconds(record.DrivenMs).ToFormattedString()),
				new ReportRow("Total Attempts", record.TotalAttempts.ToString()),
				new ReportRow("Ended", record.RanOutOfTime ? "clock ran out" : "stopped early",
					record.RanOutOfTime ? Color.Style.Status.Penalty : Color.Style.Text.Muted),
				new ReportRow("Gamemode", string.IsNullOrWhiteSpace(record.Gamemode) ? "-" : record.Gamemode)
			],
			Records = [],
			Levels = BuildLevels(levels)
		};
	}

	private static LevelRow[] BuildLevels(IReadOnlyList<RunLevelRecord> levels)
	{
		LevelRow[] rows = new LevelRow[levels.Count];

		for (int i = 0; i < levels.Count; i++)
		{
			RunLevelRecord level = levels[i];
			bool finished = level.PersonalBest >= 0;

			rows[i] = new LevelRow(i + 1,
				level.Uid,
				level.Name,
				level.Author,
				level.Status,
				StatusColour(level.Status),
				level.Attempts.ToString(),
				TimeSpan.FromMilliseconds(level.DurationMs).ToFormattedString(),
				TimeFormatter.FormatTime(level.AuthorTime),
				TimeFormatter.FormatTime(level.GoldTime),
				finished ? TimeFormatter.FormatTime(level.PersonalBest) : null,
				finished ? TimeFormatter.FormatDelta(level.PersonalBest - level.AuthorTime) : null,
				level.Crashes.ToString(),
				level.WheelsLost.ToString());
		}

		return rows;
	}

	private static string Name(string playerName)
	{
		return string.IsNullOrWhiteSpace(playerName) ? "Unknown driver" : playerName;
	}

	private static string Headlines(AthCtx ctx)
	{
		if (ctx.AuthorMedals == 0)
		{
			return "No author times this run";
		}

		return ctx.AuthorMedals == 1 ? "1 author time" : $"{ctx.AuthorMedals} author times";
	}

	private static HistoryRow[] BuildHistory(IReadOnlyList<RunRecord> history)
	{
		if (history == null || history.Count == 0)
		{
			return [];
		}

		HistoryRow[] rows = new HistoryRow[history.Count];

		for (int i = 0; i < history.Count; i++)
		{
			RunRecord record = history[i];

			rows[i] = new HistoryRow(record,
				record.EndedAt.ToString("yyyy-MM-dd HH:mm"),
				string.IsNullOrWhiteSpace(record.Gamemode) ? "-" : record.Gamemode,
				record.AuthorMedals,
				record.GoldMedals,
				record.Penalties,
				record.LevelsPlayed.ToString(),
				TimeSpan.FromMilliseconds(record.DrivenMs).ToFormattedString(),
				i == 0);
		}

		return rows;
	}

	private static ReportRow[] BuildRecords(RunStatistics stats)
	{
		List<ReportRow> rows =
		[
			new("One-Shot Author Times", stats.OneShotAuthorTimes.ToString(), Color.Style.Status.Positive),
			new("Avg. Attempts per AT", stats.AverageAttemptsPerAuthorTime.ToString("F1")),
			new("Avg. Time per AT", stats.AverageTimePerAuthorTime.ToFormattedString()),
			new("Time Wasted", stats.TotalTimeWasted.ToFormattedString(), Color.Style.Status.Bad)
		];

		if (stats.BiggestTimeSink != null)
		{
			rows.Add(new ReportRow("Biggest Time Sink",
				$"{stats.BiggestTimeSink.Name} ({stats.BiggestTimeSink.GetPlayDuration().ToFormattedString()})",
				Color.Style.Status.Bad));
		}

		if (stats.EasiestBeatenLevel != null)
		{
			rows.Add(new ReportRow("Easiest Level",
				$"{stats.EasiestBeatenLevel.Name} ({stats.EasiestBeatenLevel.Attempt} attempts)",
				Color.Style.Status.Positive));
		}

		(string author, List<Level> levels) = stats.MostBeatenAuthor;

		if (levels is { Count: > 0 })
		{
			rows.Add(new ReportRow("Most Beaten Author", $"{author} ({levels.Count}x)", Color.Style.Text.AuthorName));
		}

		return rows.ToArray();
	}

	public static LevelRow[] BuildLevels(IReadOnlyList<Level> levels)
	{
		LevelRow[] rows = new LevelRow[levels.Count];

		for (int i = 0; i < levels.Count; i++)
		{
			rows[i] = ToRow(levels[i], i + 1);
		}

		return rows;
	}

	private static LevelRow ToRow(Level level, int index)
	{
		bool finished = level.PersonalBestTime >= 0;
		double delta = level.PersonalBestTime - level.AuthorTime;

		return new LevelRow(index,
			level.LevelUid,
			level.Name,
			level.Author,
			level.StatusString,
			StatusColour(level),
			UiNumbers.Text(level.Attempt),
			level.GetPlayDuration().ToFormattedString(),
			TimeFormatter.FormatTime(level.AuthorTime),
			TimeFormatter.FormatTime(level.GoldTime),
			finished ? TimeFormatter.FormatTime(level.PersonalBestTime) : null,
			finished ? TimeFormatter.FormatDelta(delta) : null,
			UiNumbers.Text(level.Crashes),
			UiNumbers.Text(level.WheelsLost));
	}

	public static Color32 StatusColour(Level level)
	{
		return level.Status switch
		{
			LevelStatus.AUTHOR => Color.Zeepkist.Medal.Author,
			LevelStatus.GOLD => Color.Zeepkist.Medal.Gold,
			LevelStatus.FREE => Color.Style.Status.FreeSkip,
			LevelStatus.BROKEN => Color.Style.Status.Warning,
			LevelStatus.FAILED => Color.Style.Status.Penalty,
			_ => Color.Style.Text.Muted
		};
	}

	private static Color32 StatusColour(string status)
	{
		return status switch
		{
			"Completed" => Color.Zeepkist.Medal.Author,
			"Gold-Skipped" => Color.Zeepkist.Medal.Gold,
			"Free-Skipped" => Color.Style.Status.FreeSkip,
			"Broken" => Color.Style.Status.Warning,
			"Failed" => Color.Style.Status.Penalty,
			_ => Color.Style.Text.Muted
		};
	}
}
