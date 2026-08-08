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
	private const double AuthorTime = 20.0;

	private const double GoldTime = 25.0;

	[Fact]
	public void ALevelNobodyHasFinishedYetHasNoStatus()
	{
		Assert.Equal(LevelStatus.UNKNOWN, Fresh().Status);
	}

	/// <summary>Matching the author time to the millisecond claims it - the comparison is not strict.</summary>
	[Fact]
	public void AnExactAuthorTimeCountsAsTheAuthorTime()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)AuthorTime;

		Assert.Equal(LevelStatus.AUTHOR, level.Status);
		Assert.True(level.AuthorTimeAcquired);
	}

	/// <summary>The author time is a gold time too. A hunt that counted it twice would be lying.</summary>
	[Fact]
	public void TheAuthorTimeCarriesTheGoldMedalWithIt()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)AuthorTime;

		Assert.True(level.GoldMedalAcquired);
		Assert.NotEqual(LevelStatus.GOLD, level.Status);
	}

	[Fact]
	public void ATimeBetweenTheTwoMedalsIsGold()
	{
		Level level = Fresh();

		level.PersonalBestTime = (float)(AuthorTime + 1);

		Assert.Equal(LevelStatus.GOLD, level.Status);
		Assert.False(level.AuthorTimeAcquired);
	}

	[Fact]
	public void ASkipWithoutAMedalIsWhatCostsTime()
	{
		Level level = Fresh();

		level.Skipped = true;

		Assert.Equal(LevelStatus.FAILED, level.Status);
		Assert.True(level.PenaltySkipped);
	}

	[Fact]
	public void AFreeSkipIsNotAFailure()
	{
		Level level = Fresh();

		level.Skipped = true;
		level.FreeSkipped = true;

		Assert.Equal(LevelStatus.FREE, level.Status);
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

		level.PersonalBestTime = (float)AuthorTime;
		level.LevelBroken = true;

		Assert.Equal(LevelStatus.BROKEN, level.Status);
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
		Level level = new("uid", "Skyline", "Maki", AuthorTime, GoldTime);
		Level again = new("uid", "A different name", "Somebody else", 1.0, 2.0);

		Assert.Equal(level, again);
		Assert.Equal(level.GetHashCode(), again.GetHashCode());
	}

	private static Level Fresh()
	{
		return new Level("uid", "Skyline", "Maki", AuthorTime, GoldTime);
	}
}
