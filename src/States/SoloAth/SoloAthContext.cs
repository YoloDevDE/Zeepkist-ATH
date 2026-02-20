using System;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.SoloAth;

public static class SoloAthContext
{
    public static readonly TimeSpan Duration = TimeSpan.FromMinutes(2);
    public static LevelSession CurrentLevelSession { get; set; }
    public static bool IsTimeReached => DateTime.Now - StartTime >= Duration;

    public static DateTime StartTime { get; set; } = DateTime.Now;
    public static DateTime EndTime => StartTime + Duration;
}