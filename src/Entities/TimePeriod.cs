using System;

namespace AuthorTimeHunting.Entities;

public class TimePeriod
{
    public DateTime Start { get; set; }
    public DateTime End { get; set; }
    public TimeSpan Duration => End != default ? End - Start : TimeSpan.Zero;
}