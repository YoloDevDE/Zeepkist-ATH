using System;

namespace AuthorTimeHunting.Util;

public static class TimeSpanExtensions
{
	public static string ToFormattedString(this TimeSpan timeSpan)
	{
		string tmp = "";

		if (timeSpan.Days >= 1)
		{
			tmp += $"{timeSpan.Days}d ";
		}

		if (timeSpan.Hours >= 1)
		{
			tmp += $"{timeSpan.Hours}h ";
		}

		if (timeSpan.Minutes >= 1)
		{
			tmp += $"{timeSpan.Minutes}m ";
		}

		if (timeSpan.Seconds >= 1)
		{
			tmp += $"{timeSpan.Seconds}s ";
		}

		if (timeSpan.Milliseconds >= 1 && timeSpan.TotalSeconds < 10)
		{
			tmp += $"{timeSpan.Milliseconds}ms ";
		}

		return tmp.Trim();
	}
}