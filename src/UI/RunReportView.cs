using System;
using System.Collections.Generic;
using System.Linq;
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

	/// <summary>The small line above everything, saying what this screen is.</summary>
	public string Kicker { get; private set; }

	public string Headline { get; private set; }

	/// <summary>
	///     The line above the headline. A run report is the one screen in the mod that is read
	///     when nothing else is happening, and it used to open on a bare number - so it now says
	///     whose hour that was. The name comes off the lobby rather than a config field: it is
	///     the name the run was actually driven under.
	/// </summary>
	public string PlayerName { get; private set; }

	/// <summary>Every run ever finished on this machine, newest first. The one just played is at the top.</summary>
	public IReadOnlyList<HistoryRow> History { get; private set; }

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
		return From(ctx, null, null);
	}

	/// <summary>
	///     The full report: the run, who drove it, and what came before it.
	///     <paramref name="history" /> is expected to already contain this run as its first
	///     entry - the report does not record anything itself, it only shows what was recorded.
	/// </summary>
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

	/// <summary>
	///     The same screen with nothing but the history in it, for looking runs up between runs.
	///     The other tabs are empty rather than hidden: a report with a missing tab looks like a
	///     report that failed to build.
	/// </summary>
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

			// Everything ever earned rather than the last run's - under a headline that counts
			// runs, a medal row that counted only the newest one would read as the total.
			AuthorMedals = records.Sum(record => record.AuthorMedals),
			GoldMedals = records.Sum(record => record.GoldMedals),
			Penalties = records.Sum(record => record.Penalties),
			Summary = [],
			Records = [],
			Levels = []
		};
	}

	/// <summary>
	///     The name the run was driven under, verbatim, or a stand-in when the lobby has already
	///     been left - stopping a run and leaving the server in the same breath is ordinary.
	///     Nothing is put in front of it: the name is whatever the player called themselves, and
	///     a greeting bolted on turns their name into the punchline of a sentence they did not
	///     write.
	/// </summary>
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

	/// <summary>
	///     The history, formatted. The newest run is marked rather than sorted differently: it
	///     is the one just played, and finding it in a list of forty identical-looking lines is
	///     the whole reason anyone opens this tab straight after a run.
	/// </summary>
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

			rows[i] = new HistoryRow(record.EndedAt.ToString("yyyy-MM-dd HH:mm"),
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

	/// <summary>
	///     The level list, formatted. Public because the between-levels summary shows the same
	///     list in miniature, and two builders would be two lists that slowly stopped matching.
	/// </summary>
	public static LevelRow[] BuildLevels(IReadOnlyList<Level> levels)
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

	/// <summary>One past run. Medal counts stay numbers so the tab can draw them as medals.</summary>
	public readonly struct HistoryRow
	{
		public HistoryRow(string when, string gamemode, int authorMedals, int goldMedals, int penalties, string levels,
			string driven, bool isCurrent)
		{
			When = when;
			Gamemode = gamemode;
			AuthorMedals = authorMedals;
			GoldMedals = goldMedals;
			Penalties = penalties;
			Levels = levels;
			Driven = driven;
			IsCurrent = isCurrent;
		}

		public string When { get; }
		public string Gamemode { get; }
		public int AuthorMedals { get; }
		public int GoldMedals { get; }
		public int Penalties { get; }
		public string Levels { get; }
		public string Driven { get; }

		/// <summary>True for the run that was just played.</summary>
		public bool IsCurrent { get; }
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

		/// <summary>
		///     The same row under a different number. For a list that shows only the tail of a
		///     run and still has to say which levels these actually were.
		/// </summary>
		public LevelRow Renumbered(int index)
		{
			return new LevelRow(index, Name, Author, Status, StatusColour, Attempts, Duration);
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
