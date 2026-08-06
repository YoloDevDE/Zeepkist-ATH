using System.Collections.Generic;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.Ath;

/// <summary>How the level being played compares to the ones already beaten in this run.</summary>
public enum LevelPace
{
	Unknown,

	Good,

	Okay,

	Bad
}

/// <summary>
///     The level stats panel's traffic light.
///     "Am I doing badly here?" has no absolute answer - a two minute level is not a bad
///     level. What it does have is a relative one, and the run carries its own yardstick: the
///     levels already beaten. Both axes are used because either alone lies. Time alone makes
///     a long map look like a struggle; attempts alone make a short map look like one.
///     The judgement is split from the level reading on purpose. <see cref="Rate(double, int, double, double, int)" />
///     takes plain numbers and can be tested; the overload that walks a run is the adapter,
///     and has nothing in it worth testing. Play duration is measured against DateTime.Now,
///     so a test could not otherwise fabricate a level that took six minutes.
/// </summary>
public static class LevelRating
{
	public const double GoodBelow = 0.8;

	public const double BadAbove = 1.5;

	public const double TimeWeight = 0.65;

	public const double AttemptWeight = 1.0 - TimeWeight;

	public const int MinimumSamples = 2;

	public static LevelPace Rate(Level current, IReadOnlyList<Level> levels)
	{
		if (current == null || levels == null)
		{
			return LevelPace.Unknown;
		}

		double totalDuration = 0;
		double totalAttempts = 0;
		int samples = 0;

		foreach (Level level in levels)
		{
			if (level == current || level.LevelBroken || !level.AuthorTimeAcquired)
			{
				continue;
			}

			totalDuration += level.GetPlayDuration().TotalSeconds;
			totalAttempts += level.Attempt;
			samples++;
		}

		if (samples == 0)
		{
			return LevelPace.Unknown;
		}

		return Rate(current.GetPlayDuration().TotalSeconds,
			current.Attempt,
			totalDuration / samples,
			totalAttempts / samples,
			samples);
	}

	public static LevelPace Rate(double currentSeconds, int currentAttempts, double averageSeconds,
		double averageAttempts, int samples)
	{
		if (samples < MinimumSamples || averageSeconds <= 0 || averageAttempts <= 0)
		{
			return LevelPace.Unknown;
		}

		double ratio = TimeWeight * (currentSeconds / averageSeconds)
		               + AttemptWeight * (currentAttempts / averageAttempts);

		if (ratio < GoodBelow)
		{
			return LevelPace.Good;
		}

		return ratio < BadAbove ? LevelPace.Okay : LevelPace.Bad;
	}
}
