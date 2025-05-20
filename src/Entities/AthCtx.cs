using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Service;
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
    public LevelItem NextLevel => RandomLevelService.CurrentLevel;
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
            .Where(level => level.Levelbeaten)
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
        return Levels.Count(level => level.Levelbeaten && level.Attempt == 1);
    }

    /// <summary>
    ///     Calculates the average number of attempts per author time achieved
    /// </summary>
    public double AverageAttemptsPerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.Levelbeaten);
        return !beatenLevels.Any() ? 0 : beatenLevels.Average(level => level.Attempt);
    }

    /// <summary>
    ///     Calculates the average time spent per author time achieved
    /// </summary>
    public TimeSpan AverageTimePerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.Levelbeaten);
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
            .Where(level => level.Levelbeaten)
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

        if (!Plugin.Minimalist.Value)
        {
            AddDetailedWelcomeInfo(message);
        }
        else
        {
            AddMinimalistWelcomeInfo(message);
        }

        message
            .AddBreakSpace()
            .AddLine("<color=#FFD700>Good luck & have fun!</color>");

        return message.Build().ToString();
    }

    private void AddDetailedWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("Game Rules")
            .AddBreakSpace()
            .AddKeyValue("Time Limit", $"{TimeSpan.FromSeconds(Duration).ToFormattedString()}")
            .AddBreakSpace()
            .AddKeyValue("Goal", "Get as many Author Medals as possible")
            .AddBreakSpace()
            .AddSeperator("Skipping Rules")
            .AddBreakSpace()
            .AddLine("Using <#FF4500>/skip</color> gives a penalty of:")
            .AddBreakSpace()
            .AddKeyValue("Time", $"{TimeSpan.FromSeconds(PunishTime).ToFormattedString()}")
            .AddBreakSpace()
            .AddLine("No penalty if your:")
            .AddBreakSpace()
            .AddKeyValue("• Medal", $"Got <#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color> or Gold")
            .AddBreakSpace()
            .AddKeyValue("• Skip Type", $"Used <#{ColorExtension.bg_Freeskip.CTToHexRGB()}>'Free-Skip'</color>")
            .AddBreakSpace()
            .AddSeperator("Broken Maps")
            .AddBreakSpace()
            .AddLine("If AT is impossible, use:")
            .AddBreakSpace()
            .AddKeyValue("Command", "<#FF4500>/ath broken</color>")
            .AddBreakSpace()
            .AddLine("<#FF0000>Please use this responsibly!</color>");
    }

    private void AddMinimalistWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("Commands")
            .AddBreakSpace()
            .AddKeyValue("/fs", "Skip level (free)")
            .AddKeyValue("/ath broken", "Skip unbeatable map")
            .AddKeyValue("/ath restart", "Restart the hunt")
            .AddKeyValue("/ath stop", "End the hunt");
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
            .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
            .AddBreakSpace()
            .AddSeperator("Goals")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{CurrentLevel.AuthorTime.GetFormattedTime()}");

        // Show gold time if gold skip isn't unlocked yet
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
        }

        // Add current result if available
        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";

            message
                .AddBreakSpace()
                .AddKeyValue(
                    $"{(CurrentLevel.Levelbeaten ? (CurrentLevel.GoldSkipUnlocked ? "" : $"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color> ") + "Beaten by" : (CurrentLevel.GoldSkipUnlocked ? "" : $"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color> ") + "Missed by")}",
                    "<#aaaa00>" + diffDisplay + "</color>");
        }

        // Add gold time if gold skip is unlocked
        if (CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
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

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
            .AddBreakSpace()
            .AddSeperator("Result")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{CurrentLevel.AuthorTime.GetFormattedTime()}");

        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue(">Your Time", resultDisplay)
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.Levelbeaten ? "AT Beaten by" : "AT Missed by")}", diffDisplay);
        }
        else
        {
            message
                .AddBreakSpace()
                .AddKeyValue(">Your Time", resultDisplay)
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.Levelbeaten ? "Beaten by" : "Missed by")}", diffDisplay)
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
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
            .AddBreakSpace()
            .AddKeyValue($"Attempts/<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{AverageAttemptsPerAt():F2}")
            .AddBreakSpace()
            .AddKeyValue($"Time/<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{AverageTimePerAt().ToFormattedString()}")
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
            Console.WriteLine(e);
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
        builder.AddSeperator("You should have skipped this :yannics:")
            .AddBreakSpace()
            .AddLine($"{level.Name} by {level.Author}")
            .AddBreakSpace()
            .AddKeyValue("Status", $"{level.Status}")
            .AddBreakSpace()
            .AddKeyValue("Duration", $"{level.Duration.ToFormattedString()}")
            .AddBreakSpace()
            .AddKeyValue("Attempts", $"{level.Attempt}")
            .AddBreakSpace();
    }

    private void AddLikedAuthorSection(Message.Builder builder, (string Author, List<Level> Levels) likedAuthor)
    {
        builder.AddSeperator("This Author haunted you :skull:")
            .AddBreakSpace()
            .AddLine($"And their name is...<br>'{likedAuthor.Author}' !")
            .AddBreakSpace()
            .AddLine($"You've beaten {likedAuthor.Levels.Count} of their levels:")
            .AddBreakSpace();

        foreach (Level level in likedAuthor.Levels)
        {
            builder.AddLine($"- {level.Name}")
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

        Message.Builder message = new Message.Builder();

        message
            .ClearLines()
            .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}");

        if (!Plugin.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddSeperator("Result")
                .AddBreakSpace()
                .AddKeyValue("Status", CurrentLevel.Status)
                .AddBreakSpace()
                .AddKeyValue("Penalty", $"{(CurrentLevel.Levelbeaten || CurrentLevel.LevelBroken || CurrentLevel.GoldSkipUnlocked || CurrentLevel.FreeSkipped ? "0 minutes" : $"{PunishTime / 60} minutes")}");
        }

        message
            .AddBreakSpace()
            .AddSeperator("Stats");

        if (!Plugin.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{CurrentLevel.AuthorTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue(">Your Time", resultDisplay);
        }

        message
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
            .AddKeyValue("Time left", TimeFormatter.FormatDuration((int)CurrentDuration.TotalSeconds));

        return message.Build().ToString();
    }

    #endregion
}