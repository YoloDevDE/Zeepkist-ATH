using System;
using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

public class AthStats
{
    public List<PlayerLevelStats> PerLevelStats { get; set; }
    public TimeSpan MinTimeSpentOnLevel { get; set; }
    public TimeSpan MaxTimeSpentOnLevel { get; set; }
    public TimeSpan AvgTimeSpentOnLevel { get; set; }
}