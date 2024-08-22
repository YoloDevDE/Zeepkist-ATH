using System;

namespace AuthorTimeHunting.Util;

public static class TimeSpanExtensions
{
    public static string ToFormattedString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            return string.Format("{0:D1}h {1:D1}m {2:D1}s",
                (int)timeSpan.TotalHours,
                timeSpan.Minutes,
                timeSpan.Seconds);
        }

        if (timeSpan.TotalMinutes >= 1)
        {
            return string.Format("{0:D1}m {1:D1}s",
                timeSpan.Minutes,
                timeSpan.Seconds);
        }

        return string.Format("{0:D1}s",
            timeSpan.Seconds);
    }
}