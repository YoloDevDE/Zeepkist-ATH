using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Entities;

public class AthCtx
{
    public int Skips = 0;
    // Constructor (if needed)
    // You may add a constructor if you want to initialize certain properties differently.

    // Properties
    public DateTime StartTime { get; } = DateTime.Now;
    public int Duration { get; } = 60 * 15;
    public int LoadingTimeInSeconds { get; set; } = 0;

    public int PauseTimeInSeconds { get; set; } = 0;

    public int PunishTime { get; } = 60 * 5;

    public int RewardTime { get; } = 0;

    public int Punishments { set; get; } = 0;
    public int FreeSkips { get; set; } = 1;

    public bool TimeIsRunningLow { get; set; } = false;
    public Level CurrentLevel { get; set; }
    public List<Level> Levels { get; set; } = new List<Level>();


    public TimeSpan CurrentDuration => DateTime.Now.Subtract(EndTime).Duration();
    public int AuthorMedals { get; set; } = 0;

    public DateTime EndTime =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTimeInSeconds)
            .AddSeconds(LoadingTimeInSeconds)
            .AddSeconds(-(PunishTime * Punishments));

    public bool Stopped { get; set; } = false;


    public string MessageStarting()
    {
        return new Message.Builder()
            .ClearLines()
            .AddLine("ATH Ranked started. gl hf!")
            .AddBreakSpace()
            .AddSeperator()
            .AddBreakSpace()
            .AddKeyValue("Duration", $"{TimeSpan.FromSeconds(Duration).ToFormattedString()}")
            .AddBreakSpace()
            .AddKeyValue("Free-Skips", $"{FreeSkips}")
            .AddBreakSpace()
            .AddKeyValue("Reward/AT", "off")
            .AddBreakSpace()
            .AddKeyValue("Punishment", $"{TimeSpan.FromSeconds(PunishTime).ToFormattedString()}")
            .Build()
            .ToString();
    }

    public int CountTotalAttempts()
    {
        return Levels.Sum(level => level.Attempt);
    }

    public Level LevelWithLongestDuration()
    {
        return Levels
            .Where(level => level.Levelbeaten)
            .OrderByDescending(level => level.Duration)
            .FirstOrDefault();
    }

    public Level LevelWithShortestDuration()
    {
        return Levels
            .Where(level => level.Levelbeaten)
            .OrderBy(level => level.Duration)
            .FirstOrDefault();
    }

    public Level LevelThatWasVeryEasy()
    {
        return Levels
            .Where(level => level.Levelbeaten)
            .OrderBy(level => level.Attempt)
            .ThenBy(level => level.Duration)
            .FirstOrDefault();
    }

    public int CountLevelSkips()
    {
        return Levels.Count(level => level.LevelSkipped);
    }

    public int CountOneShotATs()
    {
        return Levels.Count(level => level.Levelbeaten && level.Attempt == 1);
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

    public string MessageLevelResult()
    {
        PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;

        // Initialize default values
        double result = 0;
        double positiveResult = 0;
        string diffDisplay = "--:--.---";
        string resultDisplay = "--:--.---";

        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
            resultDisplay = currentResult.Time.GetFormattedTime();
        }

        return new Message.Builder()
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
            .AddKeyValue("Total ATs", $"{(CurrentLevel.Levelbeaten ? AuthorMedals - 1 : AuthorMedals)}{(CurrentLevel.Levelbeaten ? "+1" : "")}")
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

    public string MessageFinalResult()
    {
        Level longestDurationLevel = LevelWithLongestDuration();
        Level shortestDurationLevel = LevelWithShortestDuration();
        Level easiestLevel = LevelThatWasVeryEasy();

        Message.Builder builder = new Message.Builder()
            .ClearLines()
            .AddLine("Authortime Hunt finished! :party:")
            .AddBreakSpace()
            .AddSeperator("Result")
            .AddBreakSpace()
            .AddKeyValue("Total ATs", $"{AuthorMedals}")
            .AddBreakSpace()
            .AddKeyValue("Total Resets", $"{CountTotalAttempts()}")
            .AddBreakSpace()
            .AddKeyValue("Total Skips", $"{CountLevelSkips()}")
            .AddBreakSpace()
            .AddKeyValue("ATs Oneshotted!", $"{CountOneShotATs()}")
            .AddBreakSpace();

        // Conditionally add the section for the longest duration level
        if (longestDurationLevel != null)
        {
            builder.AddSeperator("This was Time Consuming :yannics:")
                .AddBreakSpace()
                .AddLine($"{longestDurationLevel.Name} by {longestDurationLevel.Author}")
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{longestDurationLevel.Duration.ToFormattedString()}")
                .AddBreakSpace();
        }

        // Conditionally add the section for the shortest duration level
        if (shortestDurationLevel != null)
        {
            builder.AddSeperator("This was short! :smile:")
                .AddBreakSpace()
                .AddLine($"{shortestDurationLevel.Name} by {shortestDurationLevel.Author}")
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{shortestDurationLevel.Duration.ToFormattedString()}")
                .AddBreakSpace();
        }

        // Conditionally add the section for the easiest level
        if (easiestLevel != null)
        {
            builder.AddSeperator("This was easy! :coolorange:")
                .AddBreakSpace()
                .AddLine($"{easiestLevel.Name} by {easiestLevel.Author}")
                .AddBreakSpace()
                .AddKeyValue("Duration", $"{easiestLevel.Duration.ToFormattedString()}")
                .AddBreakSpace()
                .AddKeyValue("Attempts", $"{easiestLevel.Attempt}")
                .AddBreakSpace();
        }

        return builder.Build().ToString();
    }

    public string MessageLoadingCodex()
    {
        PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;

        // Initialize default values
        double result = 0;
        double positiveResult = 0;
        string diffDisplay = "--:--.---";
        string resultDisplay = "--:--.---";

        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
            resultDisplay = currentResult.Time.GetFormattedTime();
        }

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

    public bool IsTimeOver()
    {
        return DateTime.Now >= EndTime;
    }
}