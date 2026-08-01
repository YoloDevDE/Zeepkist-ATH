using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Run;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     What the level stats panel shows, as data. The counterpart to <see cref="RunHudView" />:
///     that one answers how the run is going, this one how this level is going.
/// </summary>
public class LevelStatsView
{
	private LevelStatsView()
	{
	}

	public string Name { get; private set; }
	public string Author { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	/// <summary>Best time on this level so far, or null when it has never been finished.</summary>
	public string PersonalBest { get; private set; }

	/// <summary>How far the personal best is off the author time, signed. Null without a best.</summary>
	public string AuthorDelta { get; private set; }

	public Color32 PersonalBestColour { get; private set; }

	public string Attempts { get; private set; }
	public string Crashes { get; private set; }
	public string WheelsLost { get; private set; }
	public string TimeOnLevel { get; private set; }

	public LevelPace Pace { get; private set; }

	public static LevelStatsView From(AthCtx ctx)
	{
		Level level = ctx?.CurrentLevel;

		if (level == null)
		{
			return null;
		}

		LevelStatsView view = new()
		{
			Name = level.Name,
			Author = level.Author,
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			GoldTime = TimeFormatter.FormatTime(level.GoldTime),
			Attempts = level.Attempt.ToString(),
			Crashes = level.Crashes.ToString(),
			WheelsLost = level.WheelsLost.ToString(),
			TimeOnLevel = TimeFormatter.FormatDuration((int)level.GetPlayDuration().TotalMilliseconds),
			Pace = LevelRating.Rate(level, ctx.Levels)
		};

		if (level.PersonalBestTime < 0)
		{
			return view;
		}

		double delta = level.PersonalBestTime - level.AuthorTime;

		view.PersonalBest = TimeFormatter.FormatTime(level.PersonalBestTime);
		view.AuthorDelta = $"{(delta <= 0 ? "-" : "+")}{TimeFormatter.FormatTime(Math.Abs(delta))}";
		view.PersonalBestColour = level.AuthorTimeAcquired
			? HudPalette.Author
			: level.GoldMedalAcquired
				? HudPalette.Gold
				: HudPalette.Default;

		return view;
	}

	public static string PaceLabel(LevelPace pace)
	{
		return pace switch
		{
			LevelPace.Good => "Ahead of your average",
			LevelPace.Okay => "About average",
			LevelPace.Bad => "Costing you time",
			_ => "Not enough levels yet"
		};
	}

	public static Color32 PaceColour(LevelPace pace)
	{
		return pace switch
		{
			LevelPace.Good => HudPalette.Good,
			LevelPace.Okay => HudPalette.Warning,
			LevelPace.Bad => HudPalette.Danger,
			_ => HudPalette.Muted
		};
	}
}