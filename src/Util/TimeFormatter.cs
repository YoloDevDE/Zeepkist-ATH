using System;

namespace AuthorTimeHunting.Util;

public class TimeFormatter
{
    public static string FormatDuration(int durationInSeconds)
    {
        TimeSpan timeSpan = TimeSpan.FromSeconds(durationInSeconds);

        // Überprüfe, ob Stunden vorhanden sind
        if (timeSpan.TotalHours >= 1)
            // Format für Stunden:Minuten:Sekunden
        {
            return string.Format("{0:D2}:{1:D2}:{2:D2}", (int)timeSpan.TotalHours, timeSpan.Minutes, timeSpan.Seconds);
        }

        // Format für Minuten:Sekunden
        return string.Format("{0:D2}:{1:D2}", timeSpan.Minutes, timeSpan.Seconds);
    }
}