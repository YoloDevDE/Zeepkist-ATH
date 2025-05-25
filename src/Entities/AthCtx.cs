using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Entities;

public class AthCtx
{
    // Constants
    private const int DEFAULT_DURATION = 60 * 60; // 1 hour in seconds
    private const int DEFAULT_PUNISH_TIME = 60 * 5; // 5 minutes in seconds

    // Fields
    public int Skips = 0;

    /// <summary>
    ///     Initializes a new instance of AthCtx and starts fetching the first level
    /// </summary>


    // Properties - Time related
    public DateTime StartTime { get; } = DateTime.Now;

    public int Duration { get; } = DEFAULT_DURATION;
    public int PunishTime { get; } = DEFAULT_PUNISH_TIME;
    public TimeSpan CurrentDuration => DateTime.Now.Subtract(EndTime).Duration();
    public TimeSpan CurrentDurationWithoutPunishments => DateTime.Now.Subtract(EndTimeWithoutPunishments).Duration();

    public DateTime EndTime =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTimeInSeconds)
            .AddSeconds(LoadingTimeInSeconds)
            .AddSeconds(BrokenTimeInSeconds)
            .AddSeconds(-(PunishTime * Punishments));

    public DateTime EndTimeWithoutPunishments =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTimeInSeconds)
            .AddSeconds(LoadingTimeInSeconds)
            .AddSeconds(BrokenTimeInSeconds);

    // Properties - Time adjustments
    public int LoadingTimeInSeconds { get; set; } = 0;
    public int PauseTimeInSeconds { get; set; } = 0;
    public int BrokenTimeInSeconds { get; set; } = 0;

    // Properties - Level and progress tracking
    public Level CurrentLevel { get; set; }
    public List<Level> Levels { get; set; } = new List<Level>();
    public bool TimeIsRunningLow { get; set; } = false;

    // Properties - Game statistics
    public int AuthorMedals { get; set; } = 0;
    public int GoldMedals { get; set; } = 0;
    public int Punishments { set; get; } = 0;
    public int FreeSkips { get; set; } = 1;

    public bool FirstLevel { get; set; } = true;

    #region Level Management Methods

    /// <summary>
    ///     Checks if the time for the run is over
    /// </summary>
    public bool IsTimeOver()
    {
        return DateTime.Now >= EndTime;
    }

    #endregion

    #region Level Statistics Methods

    /// <summary>
    ///     Counts the total number of level attempts across all levels
    /// </summary>
    public int CountTotalAttempts()
    {
        return Levels.Sum(level => level.Attempt);
    }

    /// <summary>
    ///     Finds the level that took the longest time (5+ minutes) and wasn't marked as broken
    /// </summary>
    public Level LevelYouShouldHaveSkippedThis()
    {
        return Levels
            .Where(level => level.Duration.TotalMinutes >= 5 && !level.LevelBroken)
            .OrderByDescending(level => level.Duration)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Finds the easiest level based on attempts and time taken
    /// </summary>
    public Level LevelThatWasVeryEasy()
    {
        return Levels
            .Where(level => level.LevelBeaten)
            .OrderBy(level => level.Attempt)
            .ThenBy(level => level.Duration)
            .FirstOrDefault();
    }

    /// <summary>
    ///     Counts how many levels were skipped
    /// </summary>
    public int CountLevelSkips()
    {
        return Levels.Count(level => level.LevelSkipped);
    }

    /// <summary>
    ///     Counts how many author times were achieved on the first attempt
    /// </summary>
    public int CountOneShotATs()
    {
        return Levels.Count(level => level.LevelBeaten && level.Attempt == 1);
    }

    /// <summary>
    ///     Calculates the average number of attempts per author time achieved
    /// </summary>
    public double AverageAttemptsPerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.LevelBeaten);
        return !beatenLevels.Any() ? 0 : beatenLevels.Average(level => level.Attempt);
    }

    /// <summary>
    ///     Calculates the average time spent per author time achieved
    /// </summary>
    public TimeSpan AverageTimePerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.LevelBeaten);
        if (!beatenLevels.Any())
        {
            return TimeSpan.Zero;
        }

        long averageTicks = (long)beatenLevels.Average(level => level.Duration.Ticks);
        return TimeSpan.FromTicks(averageTicks);
    }

    /// <summary>
    ///     Identifies an author whose levels the player has beaten multiple times
    /// </summary>
    public (string Author, List<Level> Levels) YouLikedThisAuthorALot()
    {
        return Levels
            .Where(level => level.LevelBeaten)
            .GroupBy(level => level.Author)
            .Where(group => group.Count() >= 2)
            .Select(group => (
                Author: group.Key,
                Levels: group.ToList()
            ))
            .FirstOrDefault();
    }

    #endregion

    #region Message Formatting Methods

    /// <summary>
    ///     Formats the welcome message for the start of a run
    /// </summary>
    public string MessageStarting()
    {
        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine("<#FFD700>Welcome to Author-Time-Hunting!</color>")
            .AddBreakSpace();
        if (!Plugin.Instance.Config.Minimalist.Value)
        {
            AddDetailedWelcomeInfo(message);
        }
        else
        {
            AddMinimalistWelcomeInfo(message);
        }

        message
            .AddBreakSpace()
            .AddLine("<#FFD700>Good luck & have fun!</color>");
        return message.Build().ToString();
    }

    private void AddDetailedWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("<#B336A3>Author Time Hunting</color>")
            .AddBreakSpace()
            .AddLine("<#E0E0E0>Race against time to collect as many Author Medals as possible!</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Duration</color>", $"<#FFFFFF>{TimeSpan.FromSeconds(Duration).ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddSeperator("<#64D2FF>Rules</color>")
            .AddBreakSpace()
            .AddLine($"•<indent=1em>Each level has an <#{ColorDefinitions.Author.CTToHexRGB()}>Author Medal time</color> to beat</indent>")
            .AddBreakSpace()
            .AddLine($"<indent=1em>Once you got the <#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>, the mod will skip you to the next level</indent>")
            .AddBreakSpace()
            .AddLine("<#E0E0E0>•<indent=1em>You can skip levels, but with penalties</indent></color>")
            .AddBreakSpace()
            .AddSeperator("<#FF5A5A>Skipping Rules</color>")
            .AddBreakSpace()
            .AddKeyValue("<#FF7A7A>Skip Penalty</color>", $"<#FF4040>{TimeSpan.FromSeconds(PunishTime).ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddLine("<#E0E0E0>Skip without penalty when you:</color>")
            .AddBreakSpace()
            .AddKeyValue("•<indent=1em><#E0E0E0>Got Medal</color>", $"<#{ColorDefinitions.Author.CTToHexRGB()}>Author</color> or <#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color></indent>")
            .AddBreakSpace()
            .AddKeyValue("•<indent=1em><#E0E0E0>Used</color>", $"<#{ColorDefinitions.FreeSkip.CTToHexRGB()}>Free-Skip Token</color></indent>")
            .AddBreakSpace()
            .AddSeperator("<#50E451>Commands</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>'/fs'</color>", "<#E0E0E0>Skip current level</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>'/ath broken'</color>", "<#E0E0E0>Skip unbeatable map</color>")
            .AddBreakSpace()
            .AddLine("<#a0a0a0><size=-4>Only use /ath broken for truly impossible maps!</size></color>");
    }

    private void AddMinimalistWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("<#50E451>Commands</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/fs</color>", "<#E0E0E0>Skip level (free)</color>")
            .AddKeyValue("<#7AFF7A>/ath broken</color>", "<#E0E0E0>Skip unbeatable map</color>")
            .AddKeyValue("<#7AFF7A>/ath restart</color>", "<#E0E0E0>Restart the hunt</color>")
            .AddKeyValue("<#7AFF7A>/ath stop</color>", "<#E0E0E0>End the hunt</color>");
    }

    /// <summary>
    ///     Formats the message shown during an active run
    /// </summary>
    public string MessageRunning()
    {
        PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
        // Initialize default values
        double result = 0;
        double positiveResult = 0;
        string diffDisplay = " --:--.---";
        Message.Builder message = new Message.Builder()
            .ClearLines()
            .AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Medals</color>")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>", $"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>");
        // Show gold time if gold skip isn't unlocked yet
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
        }

        // Add current result if available
        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
            string diffColor = result <= 0 ? "#50E451" : "#FF5A5A"; // Green if better, red if worse
            message
                .AddBreakSpace()
                .AddKeyValue(
                    $"{(CurrentLevel.LevelBeaten ? (CurrentLevel.GoldSkipUnlocked ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ") + "<#50E451>Beaten by</color>" : (CurrentLevel.GoldSkipUnlocked ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ") + "<#FF5A5A>Missed by</color>")}",
                    $"<{diffColor}>{diffDisplay}</color>");
        }

        // Add gold time if gold skip is unlocked
        if (CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
        }

        return message.Build().ToString();
    }

    /// <summary>
    ///     Formats the message shown after completing a level
    /// </summary>
    public string MessageLevelResult()
    {
        PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
        // Initialize default values
        double result = 0;
        double positiveResult = 0;
        string diffDisplay = " --:--.---";
        string resultDisplay = " --:--.---";
        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
            resultDisplay = currentResult.Time.GetFormattedTime();
        }

        string diffColor = result <= 0 ? "#50E451" : "#FF5A5A"; // Green if better, red if worse

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Result</color>")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>", $"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>");
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.LevelBeaten ? "<#50E451>AT Beaten by</color>" : "<#FF5A5A>AT Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>");
        }
        else
        {
            message
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.LevelBeaten ? "<#50E451>Beaten by</color>" : "<#FF5A5A>Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
        }

        return message.Build().ToString();
    }

    /// <summary>
    ///     Formats the final summary message shown at the end of a run
    /// </summary>
    public string MessageFinalResult()
    {
        Level youShouldHaveSkippedThis = LevelYouShouldHaveSkippedThis();
        Level easiestLevel = LevelThatWasVeryEasy();
        Message.Builder builder = new Message.Builder()
            .ClearLines()
            .AddLine("<#FFD700>Authortime Hunt finished! :party:</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Result</color>")
            .AddBreakSpace()
            .AddKeyValue("<#64D2FF>Total ATs</color>", $"<#{ColorDefinitions.Author.CTToHexRGB()}>{AuthorMedals}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#64D2FF>Total Resets</color>", $"<#FFFFFF>{CountTotalAttempts()}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#64D2FF>Total Skips</color>", $"<#FFFFFF>{CountLevelSkips()}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#64D2FF>ATs Oneshotted!</color>", $"<#50E451>{CountOneShotATs()}</color>")
            .AddBreakSpace()
            .AddKeyValue($"<#64D2FF>Attempts/<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color></color>", $"<#FFFFFF>{AverageAttemptsPerAt():F2}</color>")
            .AddBreakSpace()
            .AddKeyValue($"<#64D2FF>Time/<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color></color>", $"<#FFFFFF>{AverageTimePerAt().ToFormattedString()}</color>")
            .AddBreakSpace();
        try
        {
            // Add the "should have skipped" section if applicable
            if (youShouldHaveSkippedThis != null)
            {
                AddShouldHaveSkippedSection(builder, youShouldHaveSkippedThis);
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            // Handle the exception or continue execution
        }

        // Add the "liked author" section if applicable
        (string Author, List<Level> Levels) likedAuthor = YouLikedThisAuthorALot();
        if (likedAuthor.Levels != null && likedAuthor.Levels.Count > 0)
        {
            AddLikedAuthorSection(builder, likedAuthor);
        }

        return builder.Build().ToString();
    }

    private void AddShouldHaveSkippedSection(Message.Builder builder, Level level)
    {
        builder.AddSeperator("<#FF5A5A>You should have skipped this :yannics:</color>")
            .AddBreakSpace()
            .AddLine($"<#64D2FF>{level.Name}</color> by <#FFD700>{level.Author}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Status</color>", $"<#FFFFFF>{level.Status}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Duration</color>", $"<#FF7A7A>{level.Duration.ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{level.Attempt}</color>")
            .AddBreakSpace();
    }

    private void AddLikedAuthorSection(Message.Builder builder, (string Author, List<Level> Levels) likedAuthor)
    {
        builder.AddSeperator("<#50E451>This Author haunted you</color>")
            .AddBreakSpace()
            .AddLine($"<#E0E0E0>And their name is...</color><br><#FFD700>'{likedAuthor.Author}' !</color>")
            .AddBreakSpace()
            .AddLine($"<#E0E0E0>You've beaten {likedAuthor.Levels.Count} of their levels:</color>")
            .AddBreakSpace();
        foreach (Level level in likedAuthor.Levels)
        {
            builder.AddLine($"<#64D2FF>- {level.Name}</color>")
                .AddBreakSpace();
        }
    }

    /// <summary>
    ///     Formats the message shown during level loading
    /// </summary>
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

        string diffColor = result <= 0 ? "#50E451" : "#FF5A5A"; // Green if better, red if worse
        string statusColor = CurrentLevel.LevelBeaten ? "#50E451" :
            CurrentLevel.LevelBroken ? "#A0A0A0" :
            CurrentLevel.GoldSkipUnlocked ? "#FFD600" :
            CurrentLevel.FreeSkipped ? "#FFFFFF" : "#FF5A5A";

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>");
        if (!Plugin.Instance.Config.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddSeperator("<#B336A3>Result</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Status</color>", $"<{statusColor}>{CurrentLevel.Status}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Penalty</color>",
                    $"{(CurrentLevel.LevelBeaten || CurrentLevel.LevelBroken || CurrentLevel.GoldSkipUnlocked || CurrentLevel.FreeSkipped ? "<#50E451>0 minutes</color>" : $"<#FF5A5A>{PunishTime / 60} minutes</color>")}");
        }

        message
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Stats</color>");
        if (!Plugin.Instance.Config.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>", $"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>");
        }

        message
            .AddBreakSpace()
            .AddKeyValue($"{(CurrentLevel.LevelBeaten ? "<#50E451>Beaten by</color>" : "<#FF5A5A>Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{CurrentLevel.Attempt}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Duration</color>", $"<#FFFFFF>{CurrentLevel.Duration.ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Current Run</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Total ATs</color>", $"<#{ColorDefinitions.Author.CTToHexRGB()}>{AuthorMedals}</color>")
            .AddBreakSpace();

        // Use color based on remaining time
        string timeLeftColor = CurrentDuration.TotalSeconds > 300 ? "#50E451" : // Green if > 5 minutes
            CurrentDuration.TotalSeconds > 120 ? "#FFD600" : // Yellow if > 2 minutes
            "#FF5A5A"; // Red if < 2 minutes

        message.AddKeyValue("<#7FDBFF>Time left</color>", $"<{timeLeftColor}>{TimeFormatter.FormatDuration((int)CurrentDuration.TotalSeconds)}</color>");
        return message.Build().ToString();
    }

    #endregion
}