using System;

namespace AuthorTimeHunting.Util;

public static class TimeSpanExtensions
{
    public static string ToFormattedString(this TimeSpan timeSpan)
    {
        if (timeSpan.TotalHours >= 1)
        {
            // Format with hours, minutes, and seconds (without milliseconds)
            return string.Format("{0:D2}:{1:D2}:{2:D2}",
                (int)timeSpan.TotalHours,
                timeSpan.Minutes,
                timeSpan.Seconds);
        }

        // Format with minutes, seconds, and milliseconds
        return string.Format("{0:D2}:{1:D2}.{2:D3}",
            timeSpan.Minutes,
            timeSpan.Seconds,
            timeSpan.Milliseconds);
    }
}