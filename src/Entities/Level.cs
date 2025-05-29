using System;
using System.Collections.Generic;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.Entities;

public class Level
{
    private readonly List<TimePeriod> pausePeriods = new List<TimePeriod>();

    // Current pause state
    private TimePeriod _currentTimePeriod;

    public Level(LevelScriptableObject level)
    {
        StartTime = DateTime.Now;
        EndTime = StartTime;
        // Initialize immutable properties
        LevelUid = level.UID;
        Name = level.Name;
        Author = level.Author;
        AuthorTime = level.TimeAuthor;
        GoldTime = level.TimeGold;
    }

    // Basic level information (immutable after creation)
    public string LevelUid { get; }
    public string Name { get; }
    public string Author { get; }
    public double AuthorTime { get; }
    public double GoldTime { get; }

    // Game state
    public int Attempt { get; set; } = 1;
    public int Crashes { get; set; } = 0;

    // Instead of directly accumulating pause seconds, we'll calculate it from the pause periods
    public int PauseDurationInSeconds => (int)GetTotalPauseDuration().TotalSeconds;

    public bool LevelBeaten { get; set; }
    public bool LevelBroken { get; set; }
    public bool LevelSkipped { get; set; }
    public bool GoldSkipUnlocked { get; set; }
    public bool FreeSkipped { get; set; }
    public float PersonalBestTime { get; set; }
    public TimeSpan TimeWasted => LevelBroken ? TimeSpan.Zero : LevelBeaten ? PlayDuration - TimeSpan.FromSeconds(PersonalBestTime) : PlayDuration;

    // Timing
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; }

    // Calculate the total duration excluding pauses
    public TimeSpan PlayDuration => EndTime - StartTime - GetTotalPauseDuration();
    public TimeSpan TotalDuration => EndTime - StartTime;

    public string Status => LevelBeaten ? "Completed" :
        LevelBroken ? "Lvl Broken" :
        GoldSkipUnlocked ? "Gold Skipped" :
        FreeSkipped ? "Free Skipped" : "Failed";

    // Start a new pause period when the player crosses the finish line
    public void StartPause()
    {
        // If there's already an active pause, ignore this call
        if (_currentTimePeriod != null)
        {
            return;
        }

        _currentTimePeriod = new TimePeriod { Start = DateTime.Now };
        Logger.LogDebug($"Level {Name}: Started pause at {_currentTimePeriod.Start:HH:mm:ss.fff}");
    }

    // End the current pause period when the player spawns for a new attempt
    public void EndPause()
    {
        // If there's no active pause, ignore this call
        if (_currentTimePeriod == null)
        {
            return;
        }

        _currentTimePeriod.End = DateTime.Now;
        Logger.LogDebug($"Level {Name}: Ended pause at {_currentTimePeriod.End:HH:mm:ss.fff}, duration: {_currentTimePeriod.Duration.TotalSeconds:F2}s");

        // Only add valid pause periods (where end time is after start time)
        if (_currentTimePeriod.End > _currentTimePeriod.Start)
        {
            pausePeriods.Add(_currentTimePeriod);
            Logger.LogInfo($"Level {Name}: Added pause period of {_currentTimePeriod.Duration.TotalSeconds:F2}s. Total pauses: {pausePeriods.Count}, Total pause time: {GetTotalPauseDuration().TotalSeconds:F2}s");
        }

        _currentTimePeriod = null;
    }

    public void UpdatePause()
    {
        // If there's no active pause, ignore this call
        if (_currentTimePeriod == null)
        {
            return;
        }

        _currentTimePeriod.End = DateTime.Now;
        EndTime = DateTime.Now;
    }

    // Calculate the total duration of all pause periods
    private TimeSpan GetTotalPauseDuration()
    {
        TimeSpan total = TimeSpan.Zero;

        // Add all completed pause periods
        foreach (TimePeriod period in pausePeriods)
        {
            total = total.Add(period.Duration);
        }

        // If there's an active pause, add its current duration
        if (_currentTimePeriod != null && _currentTimePeriod.Start != default)
        {
            DateTime endTime = _currentTimePeriod.End != default ? _currentTimePeriod.End : DateTime.Now;
            total = total.Add(endTime - _currentTimePeriod.Start);
        }

        return total;
    }

    public override bool Equals(object obj)
    {
        if (obj is Level other)
        {
            return LevelUid == other.LevelUid;
        }

        return false;
    }

    public override int GetHashCode()
    {
        return LevelUid.GetHashCode();
    }

    // Helper class to represent a single pause period
}