using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Ath;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     The time budget, which is the run. Everything the bar across the top of the screen says
///     about the hour is read off these four methods, and the timeline along its bottom edge is
///     drawn from the same two numbers - so a level that costs nothing here must draw nothing
///     there, or the bar disagrees with the clock sitting on it.
///     A level only accrues play time between a ResumeTiming and a PauseTiming, measured against
///     DateTime.Now. Levels built here are never resumed, so they cost nothing by construction
///     and a run spends its hour purely through penalties - which is the arithmetic worth
///     pinning, and the only part of it a test can state exactly.
/// </summary>
public class AthCtxTests
{
	private const int _hour = 60 * 60 * 1000;

	private const int _penalty = 2 * 60 * 1000;

	private const double _authorTime = 10.0;

	private const double _goldTime = 15.0;

	[Fact]
	public void AFreshRunHasItsWholeBudgetLeft()
	{
		AthCtx ctx = Run();

		Assert.Equal(TimeSpan.FromMilliseconds(_hour), ctx.GetRemainingTime());
		Assert.False(ctx.IsTimeOver());
	}

	/// <summary>A penalty skip is time that was never driven and is gone all the same.</summary>
	[Fact]
	public void EveryPenaltySkipTakesItsPenaltyOffTheClock()
	{
		AthCtx ctx = Run(Failed("a"), Failed("b"));

		Assert.Equal(2, ctx.Penalties);
		Assert.Equal(2 * _penalty, ctx.GetAccumulatedPenaltyTime());
		Assert.Equal(TimeSpan.FromMilliseconds(_hour - 2 * _penalty), ctx.GetRemainingTime());
	}

	/// <summary>Gold and free skips cost nothing, which is the whole reason for having them.</summary>
	[Fact]
	public void GoldAndFreeSkipsAreFree()
	{
		AthCtx ctx = Run(GoldSkipped("a"), FreeSkipped("b"));

		Assert.Equal(0, ctx.Penalties);
		Assert.Equal(TimeSpan.FromMilliseconds(_hour), ctx.GetRemainingTime());
	}

	/// <summary>
	///     A level reported broken is refunded in full: the time spent on it never counts against
	///     the budget. The timeline leaves it out for the same reason.
	/// </summary>
	[Fact]
	public void ABrokenLevelIsLeftOutOfThePlayedTimeEvenAfterItWasDriven()
	{
		Level broken = Broken("a");
		broken.Start();
		broken.ResumeTiming();

		AthCtx ctx = Run(broken);

		Assert.True(broken.GetPlayDuration() >= TimeSpan.Zero);
		Assert.Equal(TimeSpan.Zero, ctx.GetTotalLevelPlayDuration());
		Assert.Equal(TimeSpan.FromMilliseconds(_hour), ctx.GetRemainingTime());
	}

	/// <summary>
	///     The medal tally on the bar. Author and gold are separate counts rather than one
	///     nested in the other, so a level cannot show up in both.
	/// </summary>
	[Fact]
	public void TheTallyCountsEachLevelUnderOneMedalOnly()
	{
		AthCtx ctx = Run(Beaten("a"), Beaten("b"), GoldSkipped("c"), Failed("d"), Broken("e"));

		Assert.Equal(2, ctx.AuthorMedals);
		Assert.Equal(1, ctx.GoldMedals);
		Assert.Equal(1, ctx.Penalties);
	}

	/// <summary>
	///     Time is running low once one more penalty skip would end the run. The bar turns red on
	///     this exact boundary, so it is tested on the millisecond either side of it.
	/// </summary>
	[Fact]
	public void TimeRunningLowStartsAtExactlyOnePenaltyLeft()
	{
		Assert.False(Budget(_penalty + 1).IsTimeRunningLow);
		Assert.True(Budget(_penalty).IsTimeRunningLow);
	}

	[Fact]
	public void TimeRunningLowAfterASkipStartsOnePenaltyEarlier()
	{
		Assert.False(Budget(2 * _penalty + 1).IsTimeAfterSkipRunningLow);
		Assert.True(Budget(2 * _penalty).IsTimeAfterSkipRunningLow);
	}

	[Fact]
	public void TheRunIsOverOnlyOnceTheBudgetIsActuallyGone()
	{
		Assert.False(Budget(1).IsTimeOver());
		Assert.True(Budget(0).IsTimeOver());
	}

	/// <summary>
	///     The warning speaks on the crossing and not once a frame afterwards. It is read from
	///     Update, so a plain IsTimeRunningLow would announce the same thing sixty times a second.
	/// </summary>
	[Fact]
	public void TheTimeRunningLowWarningSpeaksOncePerCrossing()
	{
		AthCtx ctx = Budget(_penalty);

		Assert.True(ctx.CheckAndNotifyTimeRunningLow());
		Assert.False(ctx.CheckAndNotifyTimeRunningLow());
	}

	[Fact]
	public void TheTimeRunningLowWarningSaysNothingWhileThereIsTime()
	{
		Assert.False(Budget(_hour).CheckAndNotifyTimeRunningLow());
	}

	/// <summary>
	///     What the run would have had left had it never been punished. The results screen puts
	///     the two side by side, so they must differ by exactly the penalties.
	/// </summary>
	[Fact]
	public void TheUnpunishedClockIgnoresPenaltiesAndNothingElse()
	{
		AthCtx ctx = Run(Failed("a"));

		Assert.Equal(TimeSpan.FromMilliseconds(_hour), ctx.GetRemainingTimeWithoutPunishments());
		Assert.Equal(TimeSpan.FromMilliseconds(_hour - _penalty), ctx.GetRemainingTime());
	}

	#region Builders

	private static AthCtx Run(params Level[] levels)
	{
		AthCtx ctx = Budget(_hour);

		ctx.Levels.AddRange(levels);

		return ctx;
	}

	/// <summary>A run of exactly this many milliseconds and nothing played, so that is what is left.</summary>
	private static AthCtx Budget(int remainingMilliseconds)
	{
		return new AthCtx(new RunSettings { DurationMs = remainingMilliseconds, PenaltyTimeMs = _penalty });
	}

	private static Level Level(string uid)
	{
		return new Level(uid, $"Level {uid}", "Alice", _authorTime, _goldTime);
	}

	/// <summary>Author time claimed: a personal best at or below the author time.</summary>
	private static Level Beaten(string uid)
	{
		Level level = Level(uid);
		level.PersonalBestTime = (float)_authorTime;
		return level;
	}

	/// <summary>Gold claimed but not author: a personal best between the two.</summary>
	private static Level GoldSkipped(string uid)
	{
		Level level = Level(uid);
		level.PersonalBestTime = (float)(_authorTime + 1);
		level.Skipped = true;
		return level;
	}

	/// <summary>Skipped without a medal, which is the only thing that costs time.</summary>
	private static Level Failed(string uid)
	{
		Level level = Level(uid);
		level.Skipped = true;
		return level;
	}

	private static Level FreeSkipped(string uid)
	{
		Level level = Level(uid);
		level.Skipped = true;
		level.FreeSkipped = true;
		return level;
	}

	private static Level Broken(string uid)
	{
		Level level = Level(uid);
		level.LevelBroken = true;
		return level;
	}

	#endregion
}
