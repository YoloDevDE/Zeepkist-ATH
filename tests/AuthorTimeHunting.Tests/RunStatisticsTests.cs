using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     RunStatistics is pure: it reads a list of levels and returns numbers. That makes it
///     the one part of the run that can be exercised without the game running, which is why
///     Level gained a constructor that does not need a LevelScriptableObject.
///     Anything derived from Level.GetPlayDuration() is deliberately only tested for its
///     empty and null cases - play duration is measured against DateTime.Now, so a test
///     cannot fabricate a level that took six minutes without a clock seam.
/// </summary>
public class RunStatisticsTests
{
	private const double _authorTime = 10.0;
	private const double _goldTime = 15.0;

	/// <summary>
	///     The regression guard for the crash that used to take the whole end-of-run summary
	///     down: AvgAuthorTime() called Average() on an empty sequence, so a run that ended
	///     without a single playable level threw instead of reporting.
	/// </summary>
	[Fact]
	public void EmptyRun_ReportsNeutralValuesInsteadOfThrowing()
	{
		RunStatistics stats = new(new List<Level>());

		Assert.Equal(0, stats.TotalAttempts);
		Assert.Equal(0, stats.OneShotAuthorTimes);
		Assert.Equal(0, stats.PenaltySkipCount);
		Assert.Equal(0, stats.AverageAuthorTime);
		Assert.Equal(0, stats.AverageAttemptsPerAuthorTime);
		Assert.Equal(TimeSpan.Zero, stats.AverageTimePerAuthorTime);
		Assert.Equal(TimeSpan.Zero, stats.TotalTimeWasted);
		Assert.Null(stats.BiggestTimeSink);
		Assert.Null(stats.EasiestBeatenLevel);
		Assert.Null(stats.MostBeatenAuthor.Levels);
	}

	[Fact]
	public void NullLevelList_IsTreatedAsAnEmptyRun()
	{
		RunStatistics stats = new(null);

		Assert.Equal(0, stats.TotalAttempts);
		Assert.Null(stats.EasiestBeatenLevel);
	}

	[Fact]
	public void TotalAttempts_SumsAcrossEveryLevelIncludingBrokenOnes()
	{
		RunStatistics stats =
			new(new List<Level> { Beaten("a", attempts: 3), Failed("b", attempts: 7), Broken("c", attempts: 2) });

		Assert.Equal(12, stats.TotalAttempts);
	}

	/// <summary>
	///     Broken levels never really got played, so they must not drag the difficulty
	///     readout around.
	/// </summary>
	[Fact]
	public void AverageAuthorTime_ExcludesBrokenLevels()
	{
		RunStatistics stats = new(new List<Level>
		{
			Beaten("a", authorTime: 10), Failed("b", authorTime: 20), Broken("c", authorTime: 300)
		});

		Assert.Equal(15.0, stats.AverageAuthorTime);
	}

	[Fact]
	public void OneShotAuthorTimes_CountsOnlyAuthorTimesTakenOnTheFirstAttempt()
	{
		RunStatistics stats =
			new(new List<Level> { Beaten("a", attempts: 1), Beaten("b", attempts: 2), Failed("c", attempts: 1) });

		Assert.Equal(1, stats.OneShotAuthorTimes);
	}

	/// <summary>
	///     Only a penalty skip counts. Gold and free skips are free by design, and a broken
	///     level is refunded - lumping them together would overstate what the run cost.
	/// </summary>
	[Fact]
	public void PenaltySkipCount_IgnoresGoldFreeAndBrokenSkips()
	{
		RunStatistics stats = new(new List<Level>
		{
			Failed("a"),
			Failed("b"),
			GoldSkipped("c"),
			FreeSkipped("d"),
			Broken("e")
		});

		Assert.Equal(2, stats.PenaltySkipCount);
	}

	[Fact]
	public void AverageAttemptsPerAuthorTime_LooksOnlyAtLevelsThatWereBeaten()
	{
		RunStatistics stats = new(new List<Level>
		{
			Beaten("a", attempts: 2), Beaten("b", attempts: 4), Failed("c", attempts: 99)
		});

		Assert.Equal(3.0, stats.AverageAttemptsPerAuthorTime);
	}

	[Fact]
	public void AverageTimePerAuthorTime_IsZeroWhenNothingWasBeaten()
	{
		RunStatistics stats = new(new List<Level> { Failed("a"), Broken("b") });

		Assert.Equal(TimeSpan.Zero, stats.AverageTimePerAuthorTime);
	}

	[Fact]
	public void EasiestBeatenLevel_PrefersTheFewestAttempts()
	{
		Level easiest = Beaten("easy", attempts: 1);

		RunStatistics stats = new(new List<Level> { Beaten("hard", attempts: 9), easiest, Beaten("medium", attempts: 4) });

		Assert.Same(easiest, stats.EasiestBeatenLevel);
	}

	[Fact]
	public void EasiestBeatenLevel_IsNullWhenNoAuthorTimeWasClaimed()
	{
		RunStatistics stats = new(new List<Level> { Failed("a"), FreeSkipped("b") });

		Assert.Null(stats.EasiestBeatenLevel);
	}

	/// <summary>
	///     A level only qualifies once it has eaten more than five minutes. Nothing built here
	///     has any measured play time, so nothing qualifies - which is exactly the case the
	///     end-of-run summary has to survive, because it skips the section on null.
	/// </summary>
	[Fact]
	public void BiggestTimeSink_IsNullWhileNoLevelCrossesTheThreshold()
	{
		RunStatistics stats = new(new List<Level> { Beaten("a", attempts: 40), Failed("b", attempts: 40) });

		Assert.Null(stats.BiggestTimeSink);
	}

	[Fact]
	public void MostBeatenAuthor_NeedsTwoBeatenLevelsFromTheSameAuthor()
	{
		RunStatistics stats = new(new List<Level> { Beaten("a"), Beaten("b", "Bob") });

		Assert.Null(stats.MostBeatenAuthor.Levels);
	}

	[Fact]
	public void MostBeatenAuthor_FindsTheAuthorWhoShowedUpTwice()
	{
		Level first = Beaten("a");
		Level second = Beaten("b");

		RunStatistics stats = new(new List<Level> { first, Beaten("c", "Bob"), second });

		(string Author, List<Level> Levels) haunting = stats.MostBeatenAuthor;

		// Grouping happens on Level.Author, which wraps the raw name in <noparse>. Comparing
		// against the entity keeps the test about the grouping rather than the formatting.
		Assert.Equal(first.Author, haunting.Author);
		Assert.Equal(new[] { first, second }, haunting.Levels);
	}

	/// <summary>
	///     Levels that were merely skipped do not count as beaten, however often the author
	///     turns up.
	/// </summary>
	[Fact]
	public void MostBeatenAuthor_IgnoresLevelsThatWereNotBeaten()
	{
		RunStatistics stats = new(new List<Level> { Failed("a"), Failed("b") });

		Assert.Null(stats.MostBeatenAuthor.Levels);
	}

	#region Builders

	private static Level Level(string uid, string author, double authorTime)
	{
		return new Level(uid, $"Level {uid}", author, authorTime, authorTime + (_goldTime - _authorTime));
	}

	/// <summary>Author time claimed: a personal best at or below the author time.</summary>
	private static Level Beaten(string uid, string author = "Alice", int attempts = 1, double authorTime = _authorTime)
	{
		Level level = Level(uid, author, authorTime);
		level.Attempt = attempts;
		level.PersonalBestTime = (float)authorTime;
		return level;
	}

	/// <summary>Gold claimed but not author: a personal best between the two.</summary>
	private static Level GoldSkipped(string uid, string author = "Alice")
	{
		Level level = Level(uid, author, _authorTime);
		level.Attempt = 1;
		level.PersonalBestTime = (float)(_authorTime + 1);
		level.Skipped = true;
		return level;
	}

	/// <summary>Skipped without a medal, which is what costs time.</summary>
	private static Level Failed(string uid, string author = "Alice", int attempts = 1, double authorTime = _authorTime)
	{
		Level level = Level(uid, author, authorTime);
		level.Attempt = attempts;
		level.Skipped = true;
		return level;
	}

	private static Level FreeSkipped(string uid, string author = "Alice")
	{
		Level level = Level(uid, author, _authorTime);
		level.Attempt = 1;
		level.Skipped = true;
		level.FreeSkipped = true;
		return level;
	}

	private static Level Broken(string uid, string author = "Alice", int attempts = 1, double authorTime = _authorTime)
	{
		Level level = Level(uid, author, authorTime);
		level.Attempt = attempts;
		level.LevelBroken = true;
		return level;
	}

	#endregion
}
