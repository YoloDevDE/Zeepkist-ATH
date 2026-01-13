using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

public class Level
{
    public enum LevelStatus
    {
        AUTHOR = 0,
        GOLD = 1,
        FREE = 2,
        FAILED = 3,
        BROKEN = 4,
        UNKOWN = 5
    }

    private readonly string _author;
    private readonly string _name;
    private DateTime _endTime;
    private float _personalBestTime = -1f;

    public Level(LevelScriptableObject level)
    {
        // Initialize immutable properties
        LevelUid = level.UID;
        _name = level.Name;
        _author = level.Author;
        AuthorTime = level.TimeAuthor;
        GoldTime = level.TimeGold;
    }

    // Basic level information (immutable after creation)
    public string LevelUid { get; }

    public string Name => $"<noparse>{_name}</noparse>";
    public string Author => $"<noparse>{_author}</noparse>";
    public double AuthorTime { get; }
    public double GoldTime { get; }

    // Game state
    public int Attempt { get; set; }
    public int Crashes { get; set; }
    public bool Skipped { get; set; }
    public bool FreeSkipped { get; set; }
    public bool LevelBroken { get; set; }

    // Status als zentrale Eigenschaft
    public LevelStatus Status
    {
        get
        {
            if (LevelBroken)
            {
                return LevelStatus.BROKEN;
            }

            if (PersonalBestTime <= AuthorTime && PersonalBestTime >= 0)
            {
                return LevelStatus.AUTHOR;
            }

            if (PersonalBestTime <= GoldTime && PersonalBestTime >= 0)
            {
                return LevelStatus.GOLD;
            }

            if (FreeSkipped)
            {
                return LevelStatus.FREE;
            }

            if (Skipped)
            {
                return LevelStatus.FAILED;
            }

            {
                return LevelStatus.UNKOWN;
            }
        }
    }

    // Boolean Properties basierend auf Status
    public bool AuthorTimeAcquired => Status == LevelStatus.AUTHOR;
    public bool GoldMedalAcquired => Status is LevelStatus.GOLD or LevelStatus.AUTHOR;
    public bool GoldSkipped => Status == LevelStatus.GOLD && Skipped;
    public bool PenaltySkipped => Status == LevelStatus.FAILED && Skipped;

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

    public TimeSpan TimeWasted =>
        LevelBroken
            ? TimeSpan.Zero
            : AuthorTimeAcquired
                ? GetPlayDuration() - TimeSpan.FromSeconds(PersonalBestTime)
                : GetPlayDuration();

    public string StatusString
    {
        get
        {
            return Status switch
            {
                LevelStatus.AUTHOR => "Completed",
                LevelStatus.GOLD => "Gold-Skipped",
                LevelStatus.FREE => "Free-Skipped",
                LevelStatus.BROKEN => "Broken",
                LevelStatus.FAILED => "Failed",
                _ => "Unknown"
            };
        }
    }

    // Rest der Klasse bleibt gleich...
    public DateTime StartTime { get; set; }

    public DateTime EndTime
    {
        get => _endTime == default ? DateTime.Now : _endTime;
        set => _endTime = value;
    }

    private List<DateTime> TimeStamps { get; } = [];


    public void AddTimeStamp()
    {
        TimeStamps.Add(DateTime.Now);
    }

    public TimeSpan GetTotalDuration() => GetPlayDuration();

    public void Start()
    {
        StartTime = DateTime.Now;
        TimeStamps.Clear();
        Attempt++;
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

        DateTime now = DateTime.Now;
        TimeSpan result = TimeSpan.Zero;

        if (TimeStamps.Count == 1)
        {
            TimeSpan duration = now - TimeStamps[0];
            return duration > TimeSpan.Zero ? duration : TimeSpan.Zero;
        }

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

    public override int GetHashCode() => LevelUid.GetHashCode();

    public TimeSpan GetPauseDuration() => GetTotalDuration() - GetPlayDuration();
}