using System.Collections.Generic;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.Run;

/// <summary>How the level being played compares to the ones already beaten in this run.</summary>
public enum LevelPace
{
	/// <summary>Not enough beaten levels yet to compare against.</summary>
	Unknown,

	/// <summary>Going faster than this run's own average.</summary>
	Good,

	/// <summary>About average for this run.</summary>
	Okay,

	/// <summary>Costing noticeably more than a level usually does.</summary>
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
	/// <summary>
	///     Below this share of the run's average, the level is going well. Loose on purpose -
	///     a light that goes green only for a personal best is a light that is never green.
	/// </summary>
	public const double GoodBelow = 0.8;

	/// <summary>Above this, the level is costing real money.</summary>
	public const double BadAbove = 1.5;

	/// <summary>
	///     Time carries most of the weight. It is what the run actually spends; attempts are a
	///     proxy for it that varies wildly with how long a lap is.
	/// </summary>
	public const double TimeWeight = 0.65;

	public const double AttemptWeight = 1.0 - TimeWeight;

	/// <summary>
	///     Two beaten levels is the fewest that is worth calling an average. With one, the
	///     light would swing between green and red on the run's second map.
	/// </summary>
	public const int MinimumSamples = 2;

	/// <summary>Rates the current level against every level this run has already beaten.</summary>
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
			// Only levels that were actually beaten. A skipped level says nothing about how
			// long beating one takes, and a broken level had its time refunded.
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

	/// <summary>
	///     The judgement itself. Returns <see cref="LevelPace.Unknown" /> rather than guessing
	///     whenever the baseline cannot carry a comparison - too few samples, or an average of
	///     zero, which happens on the first level of a run before any time has been spent.
	/// </summary>
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