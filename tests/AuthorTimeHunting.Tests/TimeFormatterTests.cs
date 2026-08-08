using AuthorTimeHunting.Util;
using Xunit;

namespace AuthorTimeHunting.Tests;

/// <summary>
///     Every clock the mod puts on screen comes out of here - the hour on the bar, the two medal
///     times beside the level, the delta under the ticker. The widths are the point as much as
///     the numbers: these are read out of the corner of an eye at speed, and a column that
///     changes width between two frames is read as a jump rather than as a digit.
/// </summary>
public class TimeFormatterTests
{
	/// <summary>
	///     Below a minute the milliseconds are there, above it they are gone. The hour on the bar
	///     crosses this once per run.
	/// </summary>
	[Fact]
	public void ADurationShowsMillisecondsOnlyWhileItIsUnderAMinute()
	{
		Assert.Equal("00:59.999", TimeFormatter.FormatDuration(59_999));
		Assert.Equal("01:00", TimeFormatter.FormatDuration(60_000));
	}

	[Fact]
	public void ADurationGrowsAnHourColumnOnlyOnceThereIsAnHour()
	{
		Assert.Equal("59:59", TimeFormatter.FormatDuration(59 * 60_000 + 59_000));
		Assert.Equal("01:00:00", TimeFormatter.FormatDuration(60 * 60_000));
	}

	/// <summary>A budget that has run out says so, rather than counting backwards past zero.</summary>
	[Fact]
	public void ADurationBelowZeroIsNotADuration()
	{
		Assert.Equal("none", TimeFormatter.FormatDuration(-1));
	}

	/// <summary>
	///     A lap time keeps its milliseconds at every length. This is the one number in the mod
	///     that is compared against another to the thousandth.
	/// </summary>
	[Fact]
	public void ALapTimeKeepsItsMillisecondsAtEveryLength()
	{
		Assert.Equal("00:24.148", TimeFormatter.FormatTime(24.148));
		Assert.Equal("01:00:00.000", TimeFormatter.FormatTime(60 * 60));
	}

	/// <summary>
	///     A level whose author time was never published reads as "none". Zero would read as a
	///     time, and an unbeatable one at that.
	/// </summary>
	[Fact]
	public void ALapTimeThatWasNeverSetIsNotZero()
	{
		Assert.Equal("none", TimeFormatter.FormatTime(-1));
	}

	/// <summary>
	///     Ahead of the medal is a minus, behind it a plus. Dead level is a minus too: matching
	///     the author time claims it, so the sign has to agree with the medal.
	/// </summary>
	[Fact]
	public void ADeltaIsSignedAgainstTheMedalItChases()
	{
		Assert.Equal("-00:01.749", TimeFormatter.FormatDelta(-1.749));
		Assert.Equal("+00:01.749", TimeFormatter.FormatDelta(1.749));
		Assert.Equal("-00:00.000", TimeFormatter.FormatDelta(0));
	}
}
