using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

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

	public string Headline { get; private set; }

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	/// <summary>The run at a glance: budget, what was spent, what it bought.</summary>
	public IReadOnlyList<ReportRow> Summary { get; private set; }

	/// <summary>The superlatives - the hardest level, the easiest, the author you saw most.</summary>
	public IReadOnlyList<ReportRow> Records { get; private set; }

	/// <summary>Every level the run touched, in the order they were played.</summary>
	public IReadOnlyList<LevelRow> Levels { get; private set; }

	public static RunReportView From(AthCtx ctx)
	{
		if (ctx == null)
		{
			return null;
		}

		RunStatistics stats = new(ctx.Levels);
		TimeSpan spent = ctx.GetTotalLevelPlayDuration();

		return new RunReportView
		{
			Headline = Headlines(ctx),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			Summary =
			[
				new ReportRow("Levels Played", ctx.Levels.Count.ToString()),
				new ReportRow("Author Times", ctx.AuthorMedals.ToString(), HudPalette.Author),
				new ReportRow("Gold Skips", ctx.GoldMedals.ToString(), HudPalette.Gold),
				new ReportRow("Penalty Skips", ctx.Penalties.ToString(), HudPalette.Penalty),
				new ReportRow("Time Budget", TimeSpan.FromMilliseconds(ctx.Duration).ToFormattedString()),
				new ReportRow("Time Driven", spent.ToFormattedString()),
				new ReportRow("Time Left", TimeFormatter.FormatDuration((int)ctx.GetRemainingTime().TotalMilliseconds)),
				new ReportRow("Total Attempts", stats.TotalAttempts.ToString())
			],
			Records = BuildRecords(stats),
			Levels = BuildLevels(ctx.Levels)
		};
	}

	private static string Headlines(AthCtx ctx)
	{
		if (ctx.AuthorMedals == 0)
		{
			return "No author times this run";
		}

		return ctx.AuthorMedals == 1 ? "1 author time" : $"{ctx.AuthorMedals} author times";
	}

	private static ReportRow[] BuildRecords(RunStatistics stats)
	{
		List<ReportRow> rows =
		[
			new("One-Shot Author Times", stats.OneShotAuthorTimes.ToString(), HudPalette.Positive),
			new("Avg. Attempts per AT", stats.AverageAttemptsPerAuthorTime.ToString("F1")),
			new("Avg. Time per AT", stats.AverageTimePerAuthorTime.ToFormattedString()),
			new("Time Wasted", stats.TotalTimeWasted.ToFormattedString(), HudPalette.Bad)
		];

		if (stats.BiggestTimeSink != null)
		{
			rows.Add(new ReportRow("Biggest Time Sink",
				$"{stats.BiggestTimeSink.Name} ({stats.BiggestTimeSink.GetPlayDuration().ToFormattedString()})",
				HudPalette.Bad));
		}

		if (stats.EasiestBeatenLevel != null)
		{
			rows.Add(new ReportRow("Easiest Level",
				$"{stats.EasiestBeatenLevel.Name} ({stats.EasiestBeatenLevel.Attempt} attempts)",
				HudPalette.Positive));
		}

		(string author, List<Level> levels) = stats.MostBeatenAuthor;

		if (levels is { Count: > 0 })
		{
			rows.Add(new ReportRow("Most Beaten Author", $"{author} ({levels.Count}x)", HudPalette.AuthorName));
		}

		return rows.ToArray();
	}

	private static LevelRow[] BuildLevels(IReadOnlyList<Level> levels)
	{
		LevelRow[] rows = new LevelRow[levels.Count];

		for (int i = 0; i < levels.Count; i++)
		{
			Level level = levels[i];

			rows[i] = new LevelRow(i + 1,
				level.Name,
				level.Author,
				level.StatusString,
				StatusColour(level),
				level.Attempt.ToString(),
				level.GetPlayDuration().ToFormattedString());
		}

		return rows;
	}

	private static Color32 StatusColour(Level level)
	{
		return level.Status switch
		{
			Level.LevelStatus.AUTHOR => HudPalette.Author,
			Level.LevelStatus.GOLD => HudPalette.Gold,
			Level.LevelStatus.FREE => HudPalette.FreeSkip,
			Level.LevelStatus.BROKEN => HudPalette.Warning,
			Level.LevelStatus.FAILED => HudPalette.Penalty,
			_ => HudPalette.Muted
		};
	}

	/// <summary>One label/value pair. The colour applies to the value, not the label.</summary>
	public readonly struct ReportRow
	{
		public ReportRow(string label, string value) : this(label, value, HudPalette.Default)
		{
		}

		public ReportRow(string label, string value, Color32 valueColour)
		{
			Label = label;
			Value = value;
			ValueColour = valueColour;
		}

		public string Label { get; }
		public string Value { get; }
		public Color32 ValueColour { get; }
	}

	/// <summary>One line of the level list.</summary>
	public readonly struct LevelRow
	{
		public LevelRow(int index, string name, string author, string status, Color32 statusColour, string attempts,
			string duration)
		{
			Index = index;
			Name = name;
			Author = author;
			Status = status;
			StatusColour = statusColour;
			Attempts = attempts;
			Duration = duration;
		}

		public int Index { get; }
		public string Name { get; }
		public string Author { get; }
		public string Status { get; }
		public Color32 StatusColour { get; }
		public string Attempts { get; }
		public string Duration { get; }
	}
}