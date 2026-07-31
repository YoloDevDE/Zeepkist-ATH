using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Builds the medal banner: which medal the last run earned and how far it was off the
///     author time.
///     This was MedalTextHelper, which built a TextMeshPro string and owned the machinery to
///     keep it alive on the game's RoundOverText - alpha fades, alignment overrides, a
///     re-apply every frame from a Harmony patch. All that is gone; what is left is the part
///     that was ever interesting, namely deciding what to say.
/// </summary>
public static class MedalBanner
{
	private const string Headline = "Author Time Hunting";
	private const float DisplaySeconds = 6f;

	/// <summary>
	///     Null when the run earned neither medal - the caller then shows nothing rather than
	///     an empty banner.
	/// </summary>
	public static OverlayBanner ForRun(Level level, double runTime, bool isNewMedal)
	{
		if (level == null)
		{
			return null;
		}

		Level.LevelStatus medal = ResolveRunMedal(level, runTime);

		if (medal is not (Level.LevelStatus.AUTHOR or Level.LevelStatus.GOLD))
		{
			return null;
		}

		List<OverlayLine> lines = new()
		{
			new OverlayLine(
				$"{(isNewMedal ? "NEW" : "CURRENT")} medal: {(medal == Level.LevelStatus.AUTHOR ? "AUTHOR" : "GOLD")}",
				medal == Level.LevelStatus.AUTHOR ? HudPalette.Author : HudPalette.Gold),
			new OverlayLine($"AT   {level.AuthorTime.GetFormattedTime()}", HudPalette.Author),
			SplitLine(level, runTime)
		};

		if (medal == Level.LevelStatus.AUTHOR)
		{
			lines.Add(new OverlayLine("(respawn to skip)", HudPalette.Muted));
		}

		return new OverlayBanner(Headline, lines, DisplaySeconds);
	}

	/// <summary>Plain text banner with the ATH headline, for the countdown and level info.</summary>
	public static OverlayBanner Message(string text, float displaySeconds = 0f)
	{
		return OverlayBanner.Of(Headline, displaySeconds, OverlayLine.Plain(text));
	}

	private static OverlayLine SplitLine(Level level, double runTime)
	{
		double difference = runTime - level.AuthorTime;
		string display = $"{StringUtils.GetSign(difference)}{Math.Abs(difference).GetFormattedTime()}";

		return new OverlayLine($"YOU  {display}",
			difference <= 0 ? HudPalette.Positive : HudPalette.Negative);
	}

	private static Level.LevelStatus ResolveRunMedal(Level level, double runTime)
	{
		if (runTime <= level.AuthorTime)
		{
			return Level.LevelStatus.AUTHOR;
		}

		return runTime <= level.GoldTime ? Level.LevelStatus.GOLD : Level.LevelStatus.UNKNOWN;
	}
}