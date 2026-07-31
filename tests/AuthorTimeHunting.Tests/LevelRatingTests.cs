using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     The traffic light's thresholds. They are a judgement call, so they are pinned here
///     rather than left to be eyeballed in the game - a change to the weights that flips a
///     level from green to red should be a deliberate one.
///     Everything here exercises the numeric overload. The overload that walks a run reads
///     Level.GetPlayDuration(), which is measured against DateTime.Now, so a test cannot
///     fabricate a level that took six minutes; only its guard cases are covered.
/// </summary>
public class LevelRatingTests
{
	private const double Average = 100.0;
	private const double AverageAttempts = 10.0;

	[Fact]
	public void FewerThanTwoSamples_IsUnknownRatherThanAGuess()
	{
		Assert.Equal(LevelPace.Unknown, Rate(10, 1, 1));
		Assert.Equal(LevelPace.Unknown, Rate(10, 1, 0));
	}

	[Fact]
	public void TwoSamples_IsEnoughToJudge()
	{
		Assert.NotEqual(LevelPace.Unknown, Rate(Average, (int)AverageAttempts));
	}

	/// <summary>
	///     The first level of a run: the average is zero because nothing has been spent yet.
	///     Dividing by it would produce infinity, and infinity is not a colour.
	/// </summary>
	[Fact]
	public void ZeroBaseline_IsUnknownRatherThanInfinity()
	{
		Assert.Equal(LevelPace.Unknown, LevelRating.Rate(30, 3, 0, AverageAttempts, 5));
		Assert.Equal(LevelPace.Unknown, LevelRating.Rate(30, 3, Average, 0, 5));
	}

	[Fact]
	public void WellUnderTheAverage_IsGood()
	{
		// Half the time and half the attempts of a normal level: ratio 0.5.
		Assert.Equal(LevelPace.Good, Rate(Average * 0.5, (int)(AverageAttempts * 0.5)));
	}

	[Fact]
	public void AtTheAverage_IsOkay()
	{
		Assert.Equal(LevelPace.Okay, Rate(Average, (int)AverageAttempts));
	}

	[Fact]
	public void WellOverTheAverage_IsBad()
	{
		// Twice the time and twice the attempts: ratio 2.0.
		Assert.Equal(LevelPace.Bad, Rate(Average * 2, (int)(AverageAttempts * 2)));
	}

	/// <summary>
	///     Time is weighted heavier than attempts, so a level eating time is called out even
	///     while the attempt count still looks ordinary. A short-lap map is the reverse case
	///     and is exactly why attempts alone are not trusted.
	/// </summary>
	[Fact]
	public void TimeOutweighsAttempts()
	{
		// Triple the time, average attempts: 0.65 * 3 + 0.35 * 1 = 2.3.
		Assert.Equal(LevelPace.Bad, Rate(Average * 3, (int)AverageAttempts));

		// Triple the attempts, average time: 0.65 * 1 + 0.35 * 3 = 1.7. Also bad, but it
		// takes a bigger excursion to get there.
		Assert.Equal(LevelPace.Bad, Rate(Average, (int)(AverageAttempts * 3)));

		// Double the attempts alone stays inside Okay; double the time alone does not.
		Assert.Equal(LevelPace.Okay, Rate(Average, (int)(AverageAttempts * 2)));
		Assert.Equal(LevelPace.Bad, Rate(Average * 2.4, (int)AverageAttempts));
	}

	[Fact]
	public void NullInputs_AreUnknown()
	{
		Assert.Equal(LevelPace.Unknown, LevelRating.Rate(null, new List<Level>()));
		Assert.Equal(LevelPace.Unknown, LevelRating.Rate(new Level("a", "A", "Someone", 10, 15), null));
	}

	/// <summary>
	///     A run made only of skips has no yardstick: nothing in it was ever beaten, so there
	///     is no such thing as "how long a level normally takes" yet.
	/// </summary>
	[Fact]
	public void OnlySkippedLevels_LeaveNoBaseline()
	{
		Level current = new("current", "Current", "Someone", 10, 15);

		List<Level> levels =
		[
			new Level("a", "A", "Someone", 10, 15) { Skipped = true },
			new Level("b", "B", "Someone", 10, 15) { Skipped = true, FreeSkipped = true },
			new Level("c", "C", "Someone", 10, 15) { LevelBroken = true },
			current
		];

		Assert.Equal(LevelPace.Unknown, LevelRating.Rate(current, levels));
	}

	private static LevelPace Rate(double seconds, int attempts, int samples = 5)
	{
		return LevelRating.Rate(seconds, attempts, Average, AverageAttempts, samples);
	}
}
