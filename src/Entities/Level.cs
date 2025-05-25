using System;

namespace AuthorTimeHunting.Entities;

public class Level
{
    public Level(LevelScriptableObject level)
    {
        // Initialize immutable properties
        LevelUid = level.UID;
        Name = level.Name;
        Author = level.Author;
        AuthorTime = level.TimeAuthor;
        GoldTime = level.TimeGold;

        // Initialize game state
        Attempt = 0;
        Crashes = 0;

        // Initialize status flags
        FirstTimePlayed = false;
        LevelBeaten = false;
        LevelBroken = false;
        LevelSkipped = false;
        GoldSkipUnlocked = false;

        // Set start time
        StartTime = DateTime.Now;
    }

    // Basic level information (immutable after creation)
    public string LevelUid { get; }
    public string Name { get; }
    public string Author { get; }
    public double AuthorTime { get; }
    public double GoldTime { get; }

    // Game state
    public int Attempt { get; set; }
    public int Crashes { get; set; }
    public int PauseDurationInSeconds { get; set; }

    // Level status flags
    public bool FirstTimePlayed { get; set; }
    public bool LevelBeaten { get; set; }
    public bool LevelBroken { get; set; }
    public bool LevelSkipped { get; set; }
    public bool GoldSkipUnlocked { get; set; }
    public bool FreeSkipped { get; set; }

    // Timing
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } = DateTime.MinValue;

    public TimeSpan Duration =>
        (EndTime == DateTime.MinValue ? DateTime.Now : EndTime) -
        StartTime -
        TimeSpan.FromSeconds(Math.Max(0, PauseDurationInSeconds - 1));

    public string Status => LevelBeaten ? "Completed" :
        LevelBroken ? "Lvl Broken" :
        GoldSkipUnlocked ? "Gold Skipped" :
        FreeSkipped ? "Free Skipped" : "Failed";
}