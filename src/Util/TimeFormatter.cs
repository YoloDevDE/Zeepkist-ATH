using System;

namespace AuthorTimeHunting.Util;

public abstract class TimeFormatter
{
	/// <summary>
	///     A countdown, written at the precision the moment deserves. Milliseconds only appear
	///     in the last minute: for the other fifty-nine they are three digits that change too
	///     fast to read and never mean anything, and the eye keeps going back to them anyway.
	///     Under a minute they are the whole point.
	/// </summary>
	public static string FormatDuration(int durationInMilliseconds)
	{
		// Return "none" if time is below 0
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

	/// <summary>
	///     A lap time, in the game's own mm:ss.fff shape. Seconds rather than milliseconds
	///     because that is the unit every time on a LevelScriptableObject comes in.
	/// </summary>
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

	/// <summary>
	///     A signed difference between two lap times, the way a split is written: a leading
	///     sign, then the magnitude. Ahead is negative, which is the convention every racing
	///     game uses and the opposite of what the arithmetic produces.
	/// </summary>
	public static string FormatDelta(double seconds)
	{
		string sign = seconds <= 0 ? "-" : "+";

		return sign + FormatTime(Math.Abs(seconds));
	}
}
