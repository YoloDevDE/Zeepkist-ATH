using System;
using System.Globalization;

namespace AuthorTimeHunting.Util;

public abstract class TimeFormatter
{
	public static string FormatDuration(int durationInMilliseconds)
	{
		if (durationInMilliseconds < 0)
		{
			return "none";
		}

		TimeSpan timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);

		if (timeSpan.TotalHours >= 1)
		{
			return $"{(int)timeSpan.TotalHours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}";
		}

		return timeSpan.TotalSeconds >= 60 ?
			$"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}" :
			$"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}";
	}

	public static string FormatTime(double seconds)
	{
		if (seconds < 0)
		{
			return "none";
		}

		TimeSpan span = TimeSpan.FromSeconds(seconds);

		return span.TotalHours >= 1 ?
			$"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}" :
			$"{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
	}

	public static string FormatDelta(double seconds)
	{
		string sign = seconds <= 0 ? "-" : "+";

		return sign + FormatTime(Math.Abs(seconds));
	}

	/// <summary>
	///     A gap read at a glance: signed seconds to the millisecond, without the minutes.
	///     <see cref="FormatDelta" /> writes a full clock, which is right beside a run time and
	///     three times too wide in a column of them - and a split is behind by tenths, not by
	///     minutes. A gap that does run to minutes still says so, it just says it as seconds.
	///     The invariant culture, because the decimal point has to be a point: this is read next
	///     to times the game writes, and a comma in one of the two columns is a typo on screen.
	/// </summary>
	public static string FormatGap(double seconds)
	{
		string sign = seconds < 0 ? "-" : "+";

		return sign + Math.Abs(seconds).ToString("F3", CultureInfo.InvariantCulture);
	}
}
