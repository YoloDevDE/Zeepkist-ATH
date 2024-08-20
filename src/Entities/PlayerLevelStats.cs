using System;

namespace AuthorTimeHunting.Entities;

public class PlayerLevelStats
{
    public PlayerLevelStats(Level level)
    {
        Level = level;
    }

    public Level Level { get; }
    public int ResetsDone { get; set; }
    public int TimesCrashed { get; set; }
    public int Finishes { get; set; }
    public TimeSpan TimeSpent { get; set; }
    public bool Skipped { get; set; }
    public bool Beaten { get; set; }
}