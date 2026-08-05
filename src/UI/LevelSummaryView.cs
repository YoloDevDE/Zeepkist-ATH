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

	/// <summary>
	///     The byline and the shouted status as the card draws them. Composed here because the
	///     card holds one of these for the whole loading screen and redraws it every frame.
	/// </summary>
	public string ByAuthor { get; private set; }

	public string StatusUpper { get; private set; }

	/// <summary>The time with its delta beside it, or null when the level was never finished.</summary>
	public string BestWithDelta { get; private set; }

	public Color32 StatusColour { get; private set; }

	public string AuthorTime { get; private set; }
	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string TimeHere { get; private set; }

	/// <summary>The last few levels of the run, oldest first. Includes the one just finished.</summary>
	public IReadOnlyList<LevelRow> Recent { get; private set; }

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
			ByAuthor = $"by {level.Author}",
			StatusUpper = level.StatusString?.ToUpperInvariant(),
			StatusColour = RunReportView.StatusColour(level),
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			Attempts = UiNumbers.Text(level.Attempt),
			Crashes = UiNumbers.Text(level.Crashes),
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

		view.BestWithDelta =
			$"{TimeFormatter.FormatTime(level.PersonalBestTime)}   ({TimeFormatter.FormatDelta(delta)})";

		return view;
	}

	private static LevelRow[] Tail(IReadOnlyList<Level> levels)
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

		LevelRow[] rows = RunReportView.BuildLevels(recent);

		// BuildLevels numbers from one; these are the tail of a longer run and have to keep
		// the numbers they had, or the list claims the run only ever played eight levels.
		for (int i = 0; i < rows.Length; i++)
		{
			rows[i] = rows[i].Renumbered(first + i + 1);
		}

		return rows;
	}
}
