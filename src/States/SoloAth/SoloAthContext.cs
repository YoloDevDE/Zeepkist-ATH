using System;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.SoloAth;

public static class SoloAthContext
{
    public static readonly TimeSpan TotalDuration = TimeSpan.FromMinutes(61);
    public static LevelSession CurrentLevelSession { get; set; }
    public static bool IsTimeReached => DateTime.Now - StartTime >= TotalDuration;

    public static DateTime StartTime { get; set; } = DateTime.Now;
    public static DateTime EndTime => StartTime + TotalDuration;
}