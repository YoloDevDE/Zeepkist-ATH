using System;
using AuthorTimeHunting.Entities;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     A level's Status is the single fact the whole run is scored on. The tally on the bar, the
///     colour of a timeline segment, whether a skip costs two minutes and what the results screen
///     says at the end all come off this one property, so the order it decides in matters as much
///     as the answers.
/// </summary>
public class LevelTests
{
	private const double _authorTime = 20.0;

	private const double _goldTime = 25.0;

	[Fact]
	public void ALevelNobodyHasFinishedYetHasNoStatus()
	{
		Assert.Equal(LevelStatus.Unknown, Fresh().Status);
	}

	/// <summary>Matching the author time to the millisecond claims it - the comparison is not strict.</summary>
	[Fact]
	public void AnExactAuthorTimeCountsAsTheAuthorTime()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)_authorTime;

		Assert.Equal(LevelStatus.Author, level.Status);
		Assert.True(level.AuthorTimeAcquired);
	}

	/// <summary>The author time is a gold time too. A hunt that counted it twice would be lying.</summary>
	[Fact]
	public void TheAuthorTimeCarriesTheGoldMedalWithIt()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)_authorTime;

		Assert.True(level.GoldMedalAcquired);
		Assert.NotEqual(LevelStatus.Gold, level.Status);
	}

	[Fact]
	public void ATimeBetweenTheTwoMedalsIsGold()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)(_authorTime + 1);

		Assert.Equal(LevelStatus.Gold, level.Status);
		Assert.False(level.AuthorTimeAcquired);
	}

	[Fact]
	public void ASkipWithoutAMedalIsWhatCostsTime()
	{
		Level level = Fresh();

		level.Skipped = true;

		Assert.Equal(LevelStatus.Failed, level.Status);
		Assert.True(level.PenaltySkipped);
	}

	[Fact]
	public void AFreeSkipIsNotAFailure()
	{
		Level level = Fresh();

		level.Skipped = true;
		level.FreeSkipped = true;

		Assert.Equal(LevelStatus.Free, level.Status);
		Assert.False(level.PenaltySkipped);
	}

	/// <summary>
	///     Broken is decided before anything else. A level can be reported broken after a medal was
	///     already taken on it, and the report is what the run has to act on - the time goes back.
	/// </summary>
	[Fact]
	public void BrokenOutranksAMedalAlreadyTaken()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)_authorTime;
		level.LevelBroken = true;

		Assert.Equal(LevelStatus.Broken, level.Status);
		Assert.False(level.AuthorTimeAcquired);
	}

	/// <summary>
	///     The property is a personal best, not a last time: a slower run must not overwrite a
	///     faster one, or a medal already on the board would fall back off it.
	/// </summary>
	[Fact]
	public void PersonalBestKeepsTheFastestTimeItWasGiven()
	{
		Level level = Fresh();

		level.PersonalBestTime = 25f;
		level.PersonalBestTime = 30f;
		level.PersonalBestTime = 22f;
		level.PersonalBestTime = 24f;

		Assert.Equal(22f, level.PersonalBestTime);
	}

	/// <summary>A run that did not finish reports a negative time, which is not a time.</summary>
	[Fact]
	public void PersonalBestIgnoresATimeThatIsNotOne()
	{
		Level level = Fresh();

		level.PersonalBestTime = 22f;
		level.PersonalBestTime = -1f;

		Assert.Equal(22f, level.PersonalBestTime);
	}

	/// <summary>
	///     The splits are what the split list measures every later attempt against, so they have to
	///     belong to the run the personal best belongs to. A slower attempt keeps its own time out
	///     of the record and must keep its checkpoints out too.
	/// </summary>
	[Fact]
	public void OnlyTheBestRunLeavesItsCheckpointsBehind()
	{
		Level level = Fresh();

		level.RecordRun(30f, new SplitSet([10d, 20d], [60d, 70d]));
		level.RecordRun(22f, new SplitSet([8d, 15d], [66d, 78d]));
		level.RecordRun(24f, new SplitSet([9d, 17d], [61d, 71d]));

		Assert.Equal(22f, level.PersonalBestTime);
		Assert.Equal([8d, 15d], level.BestSplits.Times);
		Assert.Equal([66d, 78d], level.BestSplits.Speeds);
	}

	/// <summary>A level nobody has finished has nothing to measure against, and says so as an empty set.</summary>
	[Fact]
	public void ALevelWithoutAFinishHasNoSplits()
	{
		Level level = Fresh();

		level.RecordRun(-1f, new SplitSet([10d], [60d]));

		Assert.Equal(0, level.BestSplits.Count);
		Assert.Equal(-1d, level.BestSplits.TimeAt(0));
	}

	/// <summary>
	///     Attempts are counted when the zeepkists are released, not when the level loads, so a
	///     level that has just come up is on none of them.
	/// </summary>
	[Fact]
	public void StartingALevelPutsItBackToNoAttemptsAndNoTime()
	{
		Level level = Fresh();

		level.Attempt = 7;
		level.ResumeTiming();
		level.Start();

		Assert.Equal(0, level.Attempt);
		Assert.False(level.IsTiming);
		Assert.Equal(TimeSpan.Zero, level.GetPlayDuration());
	}

	/// <summary>
	///     The regression guard for the pause button. Play duration is measured against
	///     DateTime.Now, so a paused level is only actually paused if the clock has stopped
	///     appearing in the sum - which is exactly what two identical readings prove.
	/// </summary>
	[Fact]
	public void APausedLevelStopsAccruingTime()
	{
		Level level = Fresh();

		level.Start();
		level.ResumeTiming();
		level.PauseTiming();

		Assert.False(level.IsTiming);
		Assert.Equal(level.GetPlayDuration(), level.GetPlayDuration());
	}

	[Fact]
	public void ARunningLevelKeepsAccruingTime()
	{
		Level level = Fresh();

		level.Start();
		level.ResumeTiming();

		TimeSpan first = level.GetPlayDuration();

		Assert.True(level.IsTiming);
		Assert.True(level.GetPlayDuration() >= first);
	}

	/// <summary>Pausing twice in a row must not open a second session that never closes.</summary>
	[Fact]
	public void PausingAnAlreadyPausedLevelChangesNothing()
	{
		Level level = Fresh();

		level.Start();
		level.ResumeTiming();
		level.PauseTiming();

		TimeSpan afterFirstPause = level.GetPlayDuration();

		level.PauseTiming();

		Assert.False(level.IsTiming);
		Assert.Equal(afterFirstPause, level.GetPlayDuration());
	}

	/// <summary>Two levels are the same level when they are the same workshop level.</summary>
	[Fact]
	public void LevelsAreComparedByTheirWorkshopIdentity()
	{
		Level level = new("uid", "Skyline", "Maki", _authorTime, _goldTime);
		Level again = new("uid", "A different name", "Somebody else", 1.0, 2.0);

		Assert.Equal(level, again);
		Assert.Equal(level.GetHashCode(), again.GetHashCode());
	}

	private static Level Fresh()
	{
		return new Level("uid", "Skyline", "Maki", _authorTime, _goldTime);
	}
}
