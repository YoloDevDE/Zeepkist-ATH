using System;

namespace AuthorTimeHunting.Util;

public abstract class TimeFormatter
{
	public static string FormatDuration(int durationInMilliseconds)
	{
		// Return "none" if time is below 0
		if (durationInMilliseconds < 0)
		{
			return "none";
		}

		TimeSpan timeSpan = TimeSpan.FromMilliseconds(durationInMilliseconds);

		// Round to nearest second
		// int totalSeconds = (int)Math.Round(timeSpan.TotalSeconds);
		// int hours = totalSeconds / 3600;
		// int minutes = totalSeconds % 3600 / 60;
		// int seconds = totalSeconds % 60;
		// int totalSeconds = (int)Math.Round(timeSpan.TotalSeconds);
		int hours = TimeSpan.FromMilliseconds(durationInMilliseconds).Hours;
		int minutes = TimeSpan.FromMilliseconds(durationInMilliseconds).Minutes;
		int seconds = TimeSpan.FromMilliseconds(durationInMilliseconds).Seconds;
		int millis = timeSpan.Milliseconds;

		// Check if hours are present
		return hours >= 1 ? $"{hours:D2}:{minutes:D2}:{seconds:D2}" : $"{minutes:D2}:{seconds:D2}.{millis:D3}";
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

		return span.TotalHours >= 1
			? $"{(int)span.TotalHours:D2}:{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}"
			: $"{span.Minutes:D2}:{span.Seconds:D2}.{span.Milliseconds:D3}";
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