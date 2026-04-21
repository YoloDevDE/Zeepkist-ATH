using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Util;

public static class TimeSpanExtensions
{
    public static TimeSpan Clamp(this TimeSpan timeSpan, TimeSpan min, TimeSpan max) => new TimeSpan(
        Math.Max(min.Ticks, Math.Min(max.Ticks, timeSpan.Ticks))
    );

    public static string ToFormattedString(this TimeSpan timeSpan)
    {
        List<string> components = new List<string>();

        if (timeSpan.Days >= 1)
        {
            components.Add($"{timeSpan.Days}d");
        }

        if (timeSpan.Hours >= 1)
        {
            components.Add($"{timeSpan.Hours}h");
        }

        if (timeSpan.Minutes >= 1)
        {
            components.Add($"{timeSpan.Minutes}m");
        }

        if (timeSpan.Seconds >= 1)
        {
            components.Add($"{timeSpan.Seconds}s");
        }

        if (timeSpan.Milliseconds >= 1 && timeSpan.TotalSeconds < 10)
        {
            components.Add($"{timeSpan.Milliseconds}ms");
        }

        return string.Join(" ", components);
    }

    public static string ToColonString(this TimeSpan timeSpan)
    {
        List<string> components = new List<string>();

        if (timeSpan.Days >= 1)
        {
            components.Add($"{timeSpan.Days:D2}:");
        }

        if (timeSpan.Hours >= 1)
        {
            components.Add($"{timeSpan.Hours:D2}:{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}");
        }
        else if (timeSpan.TotalSeconds > 60)
        {
            components.Add($"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}");
        }

        else
        {
            components.Add($"{timeSpan.Minutes:D2}:{timeSpan.Seconds:D2}.{timeSpan.Milliseconds:D3}");
        }

        return string.Join(" ", components);
    }
}