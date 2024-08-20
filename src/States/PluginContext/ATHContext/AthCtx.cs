using System;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class AthCtx
{
    // Constructor (if needed)
    // You may add a constructor if you want to initialize certain properties differently.

    // Properties
    public DateTime StartTime { get; } = DateTime.Now;
    public int Duration { get; } = 60 * 60;
    public int LoadingTime { get; set; } = 0;
    public int PauseTime { get; } = 0;
    public int PunishTime { get; } = 300;

    public int RewardTime { get; } = 0;

    public int Punishments { set; get; } = 0;
    public int FreeSkips { get; } = 1;
    public bool Multiplayer { get; } = false;

    public Level CurrentLevel { get; set; }


    // Computed Properties
    public DateTime EndTime =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTime)
            .AddSeconds(LoadingTime)
            .AddSeconds(-(PunishTime * Punishments));

    public TimeSpan CurrentDuration => DateTime.Now.Subtract(EndTime).Duration();
    public int AuthorMedals { get; set; } = 0;
}