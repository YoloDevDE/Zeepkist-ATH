using System;

namespace AuthorTimeHunting.Entities;

public class Level
{
    public Level(LevelScriptableObject level)
    {
        FirstTimePlayed = false;
        LevelUid = level.UID;
        Name = level.Name;
        Author = level.Author;
        AuthorTime = level.TimeAuthor;
        GoldTime = level.TimeGold;
        Attempt = 0;
        Crashes = 0;
        GoldSkipUnlocked = false;
        Levelbeaten = false;
        LevelBroken = false;
        LevelSkipped = false;
        StartTime = DateTime.Now;
    }

    public string LevelUid { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public double AuthorTime { get; set; }
    public double GoldTime { get; set; }

    public int Attempt { get; set; }
    public int Crashes { get; set; }

    public bool GoldSkipUnlocked { get; set; }
    public bool Levelbeaten { get; set; }
    public bool LevelBroken { get; set; }
    public bool LevelSkipped { get; set; }
    public bool FirstTimePlayed { get; set; }
    public DateTime StartTime { get; set; }
    public DateTime EndTime { get; set; } = DateTime.MinValue; // Initialize to MinValue

    public string Status => $"{(Levelbeaten ? "Completed" : LevelBroken ? "Lvl Broken" : GoldSkipUnlocked ? "Gold Skipped" : FreeSkipped ? "Free Skipped" : "Failed")}";

    public TimeSpan Duration
    {
        get
        {
            DateTime endTime = EndTime == DateTime.MinValue ? DateTime.Now : EndTime;
            return endTime - StartTime - TimeSpan.FromSeconds(PauseDurationInSeconds);
        }
    }

    public int PauseDurationInSeconds { get; set; }
    public bool FreeSkipped { get; set; }
}