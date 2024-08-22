using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class AthCtx
{
    public int Skips = 0;
    // Constructor (if needed)
    // You may add a constructor if you want to initialize certain properties differently.

    // Properties
    public DateTime StartTime { get; } = DateTime.Now;
    public int Duration { get; } = 60 * 60;
    public int LoadingTimeInSeconds { get; set; } = 0;
    public int PauseTimeInSeconds { get; } = 0;
    public int PunishTime { get; } = 60 * 5;

    public int RewardTime { get; } = 0;

    public int Punishments { set; get; } = 0;
    public int FreeSkips { get; set; } = 1;

    public Level CurrentLevel { get; set; }


    // Computed Properties
    public DateTime EndTime =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTimeInSeconds)
            .AddSeconds(LoadingTimeInSeconds)
            .AddSeconds(-(PunishTime * Punishments));

    public TimeSpan CurrentDuration => DateTime.Now.Subtract(EndTime).Duration();
    public int AuthorMedals { get; set; } = 0;

    public string MessageStarting()
    {
        return new Message.Builder()
            .ClearLines()
            .AddLine("ATH Ranked started. gl hf!")
            .AddBreakSpace()
            .AddSeperator()
            .AddBreakSpace()
            .AddKeyValue("Duration", $"{Duration / 60} min")
            .AddBreakSpace()
            .AddKeyValue("Free-Skips", $"{FreeSkips}")
            .AddBreakSpace()
            .AddKeyValue("Reward/AT", "off")
            .AddBreakSpace()
            .AddKeyValue("Punishment", $"{PunishTime / 60} min")
            .Build()
            .ToString();
    }

    public string MessageRunning()
    {
        return
            new Message.Builder()
                .ClearLines()
                .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
                .AddBreakSpace()
                .AddSeperator("Goals")
                .AddBreakSpace()
                .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue("Gold", $"{CurrentLevel.GoldTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddSeperator("Stats")
                .AddBreakSpace()
                .AddKeyValue("Total ATs", $"{AuthorMedals}")
                .AddBreakSpace()
                .AddKeyValue("Attempt", $"{CurrentLevel.Attempt}")
                .AddBreakSpace()
                .AddSeperator("Misc")
                .AddBreakSpace()
                .AddKeyValue("Gold Skip", $"{(CurrentLevel.GoldSkipUnlocked ? "unlocked :zaagbladpad:" : "locked :zaagbladpadrood:")}")
                .AddBreakSpace()
                .AddKeyValue("Free Skips", $"{FreeSkips}")
                .Build()
                .ToString();
    }

    public string MessageFinish()
    {
        double result = ZeepkistNetwork.LocalPlayer.CurrentResult.Time - CurrentLevel.AuthorTime;
        double positiveResult = Math.Abs(result);
        string diffDisplay = ZeepkistNetwork.LocalPlayer.CurrentResult != null
            ? $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}"
            : "--:--.---";
        string resultDisplay = ZeepkistNetwork.LocalPlayer.CurrentResult != null
            ? $"{ZeepkistNetwork.LocalPlayer.CurrentResult.Time.GetFormattedTime()}"
            : "--:--.---";
        return
            new Message.Builder()
                .ClearLines()
                .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
                .AddBreakSpace()
                .AddSeperator("Result")
                .AddBreakSpace()
                .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue("Your Time", resultDisplay)
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.Levelbeaten ? "Beaten by" : "Missed by")}", diffDisplay)
                .AddBreakSpace()
                .AddSeperator("Stats")
                .AddBreakSpace()
                .AddKeyValue("Total ATs", $"{AuthorMedals}{(CurrentLevel.Levelbeaten ? "+1" : "")}")
                .AddBreakSpace()
                .AddKeyValue("Attempt", $"{CurrentLevel.Attempt}{(!CurrentLevel.Levelbeaten ? "+1" : "")}")
                .AddBreakSpace()
                .AddSeperator("Misc")
                .AddBreakSpace()
                .AddKeyValue("Gold Skip", $"{(CurrentLevel.GoldSkipUnlocked ? "unlocked :zaagbladpad:" : "locked :zaagbladpadrood:")}")
                .AddBreakSpace()
                .AddKeyValue("Free Skips", $"{FreeSkips}")
                .Build()
                .ToString();
    }

    public string MessageLoadingCodex()
    {
        double result = ZeepkistNetwork.LocalPlayer.CurrentResult?.Time - CurrentLevel.AuthorTime ?? 0;
        double positiveResult = Math.Abs(result);
        string diffDisplay = ZeepkistNetwork.LocalPlayer.CurrentResult != null
            ? $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}"
            : "--:--.---";
        string resultDisplay = ZeepkistNetwork.LocalPlayer.CurrentResult != null
            ? $"{ZeepkistNetwork.LocalPlayer.CurrentResult.Time.GetFormattedTime()}"
            : "--:--.---";

        return
            new Message.Builder()
                .ClearLines()
                .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
                .AddBreakSpace()
                .AddSeperator("Result")
                .AddBreakSpace()
                .AddKeyValue("Status", $"{(CurrentLevel.Levelbeaten ? "Completed" : CurrentLevel.LevelBroken ? "Lvl Broken" : CurrentLevel.GoldSkipUnlocked ? "Gold Skipped" : CurrentLevel.FreeSkipped ? "Free Skipped" : "Failed")}")
                .AddBreakSpace()
                .AddKeyValue("Penalty", $"{(CurrentLevel.Levelbeaten || CurrentLevel.LevelBroken || CurrentLevel.GoldSkipUnlocked || CurrentLevel.FreeSkipped ? "0 minutes" : $"{PunishTime / 60} minutes")}")
                .AddBreakSpace()
                .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue("Your Time", resultDisplay)
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.Levelbeaten ? "Beaten by" : "Missed by")}", diffDisplay)
                .AddBreakSpace()
                .AddKeyValue("Attempts", $"{CurrentLevel.Attempt}")
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{CurrentLevel.Duration.ToFormattedString()}")
                .AddBreakSpace()
                .AddSeperator("Current Run")
                .AddBreakSpace()
                .AddKeyValue("Total ATs", $"{AuthorMedals}")
                .AddBreakSpace()
                .AddSeperator("Misc")
                .AddBreakSpace()
                .AddKeyValue("Free Skips", $"{FreeSkips}")
                .Build()
                .ToString();
    }
}