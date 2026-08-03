using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     What the level that just ended came to, plus the run so far, as data.
///     A snapshot: the moment the next level is drawn, AthCtx.CurrentLevel becomes a different
///     level, and this is read across exactly that boundary.
/// </summary>
public class LevelSummaryView
{
	/// <summary>
	///     How many levels the run-so-far list shows. The loading screen is a few seconds long
	///     and the point is the shape of the run, not an audit - the full list is in the report.
	/// </summary>
	private const int RecentLevels = 8;

	private LevelSummaryView()
	{
	}

	public string Name { get; private set; }
	public string Author { get; private set; }

	/// <summary>What the level ended as, in the words the report uses.</summary>
	public string Status { get; private set; }

	public Color32 StatusColour { get; private set; }

	/// <summary>The time that settled it, or null when the level was never finished.</summary>
	public string YourBest { get; private set; }

	/// <summary>How far that time was off the author time, signed. Null without a time.</summary>
	public string AuthorDelta { get; private set; }

	public string AuthorTime { get; private set; }
	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string TimeHere { get; private set; }

	/// <summary>The last few levels of the run, oldest first. Includes the one just finished.</summary>
	public IReadOnlyList<RunReportView.LevelRow> Recent { get; private set; }

	/// <summary>Where the run stands, so the summary is not only about one level.</summary>
	public string Score { get; private set; }

	public string TimeLeft { get; private set; }

	public static LevelSummaryView From(AthCtx ctx)
	{
		Level level = ctx?.CurrentLevel;

		if (level == null)
		{
			return null;
		}

		LevelSummaryView view = new()
		{
			Name = level.Name,
			Author = level.Author,
			Status = level.StatusString,
			StatusColour = ColourOf(level),
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			Attempts = level.Attempt.ToString(),
			Crashes = level.Crashes.ToString(),
			TimeHere = level.GetPlayDuration().ToFormattedString(),
			Recent = Tail(ctx.Levels),
			Score = $"{ctx.AuthorMedals} AT   {ctx.GoldMedals} gold   {ctx.Penalties} penalty",
			TimeLeft = TimeFormatter.FormatDuration((int)ctx.GetRemainingTime().TotalMilliseconds)
		};

		if (level.PersonalBestTime < 0)
		{
			return view;
		}

		double delta = level.PersonalBestTime - level.AuthorTime;

		view.YourBest = TimeFormatter.FormatTime(level.PersonalBestTime);
		view.AuthorDelta = TimeFormatter.FormatDelta(delta);

		return view;
	}

	private static RunReportView.LevelRow[] Tail(IReadOnlyList<Level> levels)
	{
		if (levels == null || levels.Count == 0)
		{
			return [];
		}

		int first = Math.Max(0, levels.Count - RecentLevels);
		List<Level> recent = new(levels.Count - first);

		for (int i = first; i < levels.Count; i++)
		{
			recent.Add(levels[i]);
		}

		RunReportView.LevelRow[] rows = RunReportView.BuildLevels(recent);

		// BuildLevels numbers from one; these are the tail of a longer run and have to keep
		// the numbers they had, or the list claims the run only ever played eight levels.
		for (int i = 0; i < rows.Length; i++)
		{
			rows[i] = rows[i].Renumbered(first + i + 1);
		}

		return rows;
	}

	private static Color32 ColourOf(Level level)
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
}
