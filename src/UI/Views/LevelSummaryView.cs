using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the level that just ended came to, plus the run so far, as data.
///     A snapshot: the moment the next level is drawn, AthCtx.CurrentLevel becomes a different
///     level, and this is read across exactly that boundary.
/// </summary>
public class LevelSummaryView
{
	private const int _recentLevels = 8;

	private LevelSummaryView()
	{
	}

	public string Name { get; private set; }

	/// <summary>What <see cref="LevelThumbnails" /> needs to find the picture.</summary>
	public string Uid { get; private set; }

	public string ByAuthor { get; private set; }

	public string StatusUpper { get; private set; }

	public string BestWithDelta { get; private set; }

	public Color32 StatusColour { get; private set; }

	public string AuthorTime { get; private set; }
	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string TimeHere { get; private set; }

	public IReadOnlyList<LevelRow> Recent { get; private set; }

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
			Uid = level.LevelUid,
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

		int first = Math.Max(0, levels.Count - _recentLevels);
		List<Level> recent = new(levels.Count - first);

		for (int i = first; i < levels.Count; i++)
		{
			recent.Add(levels[i]);
		}

		LevelRow[] rows = RunReportView.BuildLevels(recent);

		for (int i = 0; i < rows.Length; i++)
		{
			rows[i] = rows[i].Renumbered(first + i + 1);
		}

		return rows;
	}
}
