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
}