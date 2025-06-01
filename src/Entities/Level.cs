using System;
using System.Collections.Generic;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.Entities;

public class Level
{
    private DateTime _endTime;
    private float _personalBestTime = -1f;

    public Level(LevelScriptableObject level)
    {
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


    public bool LevelBeaten => PersonalBestTime <= AuthorTime && PersonalBestTime > 0;
    public bool LevelBroken { get; set; }
    public bool LevelSkipped { get; set; }
    public bool GoldSkipUnlocked { get; set; }
    public bool FreeSkipped { get; set; }

    public float PersonalBestTime
    {
        get => _personalBestTime;
        set
        {
            if (value >= 0 && (_personalBestTime <= 0 || value < _personalBestTime))
            {
                _personalBestTime = value;
            }
        }
    }

    public TimeSpan TimeWasted => LevelBroken ? TimeSpan.Zero : LevelBeaten ? GetPlayDuration() - TimeSpan.FromMilliseconds(PersonalBestTime) : GetPlayDuration();


    public string Status =>
        LevelBeaten ? "Completed" : LevelBroken ? "Lvl Broken" : GoldSkipUnlocked ? "Gold Skipped" : FreeSkipped ? "Free Skipped" : "Failed";

    public DateTime StartTime { get; set; }

    public DateTime EndTime
    {
        get => _endTime == default ? DateTime.Now : _endTime;
        set => _endTime = value;
    }

    private List<DateTime> TimeStamps { get; } = [];

    public void UnlockGoldSkip()
    {
        Messenger.Notify().LogCustomColors("Gold Medal acquired!<br>You can now skip without penalty", Color.black, new Color(1f, 0.84f, 0f), 10f);
        GoldSkipUnlocked = true;
    }

    public void AddTimeStamp()
    {
        TimeStamps.Add(DateTime.Now);
    }

    public TimeSpan GetTotalDuration()
    {
        return GetPlayDuration();
    }

    public void Start()
    {
        StartTime = DateTime.Now;
        TimeStamps.Clear();
    }

    public void Stop()
    {
        EndTime = DateTime.Now;
        if (TimeStamps.Count % 2 == 1)
        {
            TimeStamps.Add(EndTime);
        }
    }

    public TimeSpan GetPlayDuration()
    {
        if (TimeStamps == null || TimeStamps.Count == 0)
        {
            return TimeSpan.Zero;
        }

        DateTime now = DateTime.Now; // Use consistent timestamp
        TimeSpan result = TimeSpan.Zero;

        if (TimeStamps.Count == 1)
        {
            TimeSpan duration = now - TimeStamps[0];
            return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        }

        // Process pairs of timestamps (start/stop)
        for (int i = 0; i < TimeStamps.Count - 1; i++)
        {
            if (i % 2 == 1)
            {
                continue;
            }

            DateTime startTime = TimeStamps[i];
            DateTime endTime = i + 1 < TimeStamps.Count ? TimeStamps[i + 1] : now;

            TimeSpan sessionDuration = endTime - startTime;
            if (sessionDuration > TimeSpan.Zero)
            {
                result += sessionDuration;
            }
        }

        if (TimeStamps.Count % 2 == 1)
        {
            result += now - TimeStamps[^1];
        }

        return result;
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

    public TimeSpan GetPauseDuration()
    {
        return GetTotalDuration() - GetPlayDuration();
    }
}