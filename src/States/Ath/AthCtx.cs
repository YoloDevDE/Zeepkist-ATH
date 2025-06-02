using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath;

public class AthCtx
{
    // Constants
    private const int DEFAULT_DURATION_IN_MILLIS = 60 * 15 * 1000; // 1 hour in seconds
    private const int DEFAULT_PENALTY_TIME_IN_MILLIS = 60 * 5 * 1000; // 5 minutes in seconds
    private const int RETRIES = 3;
    private bool _previousTimeRunningLowState;

    public int Retries { get; set; } = RETRIES;

    /// <summary>
    ///     Initializes a new instance of AthCtx and starts fetching the first level
    /// </summary>


    public int Duration => DEFAULT_DURATION_IN_MILLIS;

    public int PenaltyTimeInMilliseconds => DEFAULT_PENALTY_TIME_IN_MILLIS;


    public Level CurrentLevel { get; set; }
    public List<Level> Levels { get; set; } = [];

    public bool HasTimeRunningLowNotified { get; set; }

    public bool IsTimeRunningLow => GetRemainingTime().TotalMilliseconds <= GetAccumulatedPenaltyTime();
    public bool IsTimeAfterSkipRunningLow => GetRemainingTime().TotalMilliseconds <= GetAccumulatedPenaltyTime() + PenaltyTimeInMilliseconds;

    public int AuthorMedals
    {
        get { return Levels?.Count(level => level.Status == Level.LevelStatus.AUTHOR) ?? 0; }
    }

    public int GoldMedals
    {
        get { return Levels?.Count(level => level.Status == Level.LevelStatus.GOLD) ?? 0; }
    }

    public int Penalties
    {
        get { return Levels?.Count(level => level.Status == Level.LevelStatus.FAILED) ?? 0; }
    }

    public int Skips
    {
        get { return Levels?.Count(level => level.Skipped) ?? 0; }
    }

    public int AvaiableFreeSkips { get; set; } = 1;

    public int GetAccumulatedPenaltyTime()
    {
        return PenaltyTimeInMilliseconds * Penalties;
    }

    public TimeSpan GetTotalLevelDuration()
    {
        return TimeSpan.FromMilliseconds(Levels.Sum(level => level.GetTotalDuration().TotalMilliseconds));
    }

    public TimeSpan GetTotalLevelPlayDuration()
    {
        return TimeSpan.FromMilliseconds(Levels.Where(level => !level.LevelBroken).Sum(level => level.GetPlayDuration().TotalMilliseconds));
    }

    public TimeSpan GetTotalLevelPauseDuration()
    {
        return TimeSpan.FromMilliseconds(Levels.Sum(level => level.GetPauseDuration().TotalMilliseconds));
    }

    public TimeSpan GetRemainingTime()
    {
        return TimeSpan.FromMilliseconds(Duration - (GetTotalLevelPlayDuration().TotalMilliseconds + GetAccumulatedPenaltyTime()));
    }

    public TimeSpan GetRemainingTimeWithoutPunishments()
    {
        return TimeSpan.FromMilliseconds(Duration - GetTotalLevelPlayDuration().TotalMilliseconds);
    }

    public void ResetRetries()
    {
        Retries = RETRIES;
    }

    #region Level Management Methods

    /// <summary>
    ///     Checks if the time for the run is over
    /// </summary>
    public bool IsTimeOver()
    {
        return GetRemainingTime() <= TimeSpan.Zero;
    }

    #endregion

    public bool CheckAndNotifyTimeRunningLow()
    {
        bool currentState = IsTimeRunningLow;
        bool shouldNotify = false;

        // Detect transition from normal to low time
        if (currentState && !_previousTimeRunningLowState)
        {
            HasTimeRunningLowNotified = true;
            shouldNotify = true;
        }
        // Reset notification when time is no longer running low
        else if (!currentState && _previousTimeRunningLowState)
        {
            HasTimeRunningLowNotified = false;
        }

        _previousTimeRunningLowState = currentState;
        return shouldNotify;
    }

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
            .Where(level => level.GetPlayDuration().TotalMinutes >= 5 && !level.LevelBroken)
            .OrderByDescending(level => level.GetPlayDuration())
            .FirstOrDefault();
    }

    public double AvgAuthorTime()
    {
        List<Level> levels = Levels.Where(x => !x.LevelBroken).ToList();
        return levels.Average(level => level.AuthorTime);
    }

    public TimeSpan TimeWastedTotal()
    {
        TimeSpan totalTime = TimeSpan.Zero;
        foreach (Level level in Levels)
        {
            totalTime = totalTime.Add(level.TimeWasted);
        }

        return totalTime;
    }

    /// <summary>
    ///     Finds the easiest level based on attempts and time taken
    /// </summary>
    public Level LevelThatWasVeryEasy()
    {
        return Levels
            .Where(level => level.AuthorTimeAcquired)
            .OrderBy(level => level.Attempt)
            .ThenBy(level => level.GetPlayDuration())
            .FirstOrDefault();
    }

    /// <summary>
    ///     Counts how many levels were skipped
    /// </summary>
    public int CountLevelSkips()
    {
        return Levels.Count(level => level.PenaltySkipped);
    }

    /// <summary>
    ///     Counts how many author times were achieved on the first attempt
    /// </summary>
    public int CountOneShotATs()
    {
        return Levels.Count(level => level.AuthorTimeAcquired && level.Attempt == 1);
    }

    /// <summary>
    ///     Calculates the average number of attempts per author time achieved
    /// </summary>
    public double AverageAttemptsPerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.AuthorTimeAcquired).ToList();
        return !beatenLevels.Any() ? 0 : beatenLevels.Average(level => level.Attempt);
    }

    /// <summary>
    ///     Calculates the average time spent per author time achieved
    /// </summary>
    public TimeSpan AverageTimePerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.AuthorTimeAcquired).ToList();
        if (!beatenLevels.Any())
        {
            return TimeSpan.Zero;
        }

        long averageTicks = (long)beatenLevels.Average(level => level.GetPlayDuration().Ticks);
        return TimeSpan.FromTicks(averageTicks);
    }

    /// <summary>
    ///     Identifies an author whose levels the player has beaten multiple times
    /// </summary>
    public (string Author, List<Level> Levels) YouLikedThisAuthorALot()
    {
        return Levels
            .Where(level => level.AuthorTimeAcquired)
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
        if (!Plugin.Instance.MyConfig.Minimalist.Value)
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

    public void InitializingNewLevel(LevelScriptableObject levelScriptableObject)
    {
        Level level = new Level(levelScriptableObject);
        CurrentLevel = level;
        Levels.Add(level);
        CurrentLevel.Start();
    }

    private void AddDetailedWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("<#B336A3>Author Time Hunting</color>")
            .AddBreakSpace()
            .AddLine("<#E0E0E0>Collect Author Medals within the time limit!</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Settings</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Duration</color>", $"<#FFFFFF>{TimeSpan.FromMilliseconds(Duration).ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#FF7A7A>Skip Penalty</color>", $"<#FF4040>{TimeSpan.FromMilliseconds(PenaltyTimeInMilliseconds).ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddSeperator("<#50E451>Commands</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/fs</color>", "<#E0E0E0>Skip level</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/ath broken</color>", "<#E0E0E0>Skip unbeatable map</color>");
    }

    private void AddMinimalistWelcomeInfo(Message.Builder message)
    {
        message
            .AddSeperator("<#50E451>Commands</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/fs</color>", "<#E0E0E0>Skip level (free)</color>").AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/ath broken</color>", "<#E0E0E0>Skip unbeatable map</color>").AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/ath restart</color>", "<#E0E0E0>Restart the hunt</color>").AddBreakSpace()
            .AddKeyValue("<#7AFF7A>/ath stop</color>", "<#E0E0E0>End the hunt</color>");
    }

    /// <summary>
    ///     Formats the message shown during an active run
    /// </summary>
    public string MessageOnARun()
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
        if (!CurrentLevel.GoldMedalAcquired)
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
            string diffColor = $"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse
            message
                .AddBreakSpace()
                .AddKeyValue(
                    $"{(CurrentLevel.AuthorTimeAcquired ? $"{(CurrentLevel.GoldMedalAcquired ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ")}<#50E451>Beaten by</color>" : $"{(CurrentLevel.GoldMedalAcquired ? "" : $"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color> ")}<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}",
                    $"<{diffColor}>{diffDisplay}</color>");
        }

        // Add gold time if gold skip is unlocked
        if (CurrentLevel.GoldMedalAcquired)
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
    public string MessageCrossedFinishLine()
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

        string diffColor = $"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Result</color>")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>", $"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>");
        if (!CurrentLevel.GoldMedalAcquired)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>AT Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>AT Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>");
        }
        else
        {
            message
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>")
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Gold.CTToHexRGB()}>Gold</color>", $"<#FFFFFF>{CurrentLevel.GoldTime.GetFormattedTime()}</color>");
        }

        return message.Build().ToString();
    }

    /// <summary>
    ///     Formats the final summary message shown at the end of a run
    /// </summary>
    public string MessageEnd()
    {
        Level youShouldHaveSkippedThis = LevelYouShouldHaveSkippedThis();
        Level easiestLevel = LevelThatWasVeryEasy();
        Message.Builder builder = new Message.Builder()
            .ClearLines()
            .AddLine("<#FFD700>Authortime Hunt finished! :party:</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Result</color>")
            .AddBreakSpace()
            // Medal Stats
            .AddKeyValue("<#64D2FF>Total ATs</color>", $"<#{ColorDefinitions.Author.CTToHexRGB()}>{AuthorMedals}</color>").AddBreakSpace()
            .AddKeyValue("<#64D2FF>ATs Oneshotted!</color>", $"<#50E451>{CountOneShotATs()}</color>")
            .AddBreakSpace()
            // Attempt Stats
            .AddKeyValue("<#64D2FF>Total Resets</color>", $"<#FFFFFF>{CountTotalAttempts()}</color>").AddBreakSpace()
            .AddKeyValue("<#64D2FF>Total Skips</color>", $"<#FFFFFF>{CountLevelSkips()}</color>").AddBreakSpace()
            .AddKeyValue("<#64D2FF>Attempts/AT</color>", $"<#FFFFFF>{AverageAttemptsPerAt():F2}</color>")
            .AddBreakSpace()
            // Time Stats  
            .AddKeyValue("<#64D2FF>Avg AT</color>", $"<#FFFFFF>{AvgAuthorTime().GetFormattedTime()}</color>").AddBreakSpace()
            .AddKeyValue("<#64D2FF>Time Wasted</color>", $"<#FF5A5A>{TimeWastedTotal().ToFormattedString()}</color>").AddBreakSpace()
            .AddKeyValue("<#64D2FF>Time/AT</color>", $"<#FFFFFF>{AverageTimePerAt().ToFormattedString()}</color>")
            .AddBreakSpace();

        try
        {
            // Add the "should have skipped" section if applicable 
            if (youShouldHaveSkippedThis != null)
            {
                AddShouldHaveSkippedSection(builder, youShouldHaveSkippedThis);
            }

            // Add the easiest level section if applicable
            if (easiestLevel != null)
            {
                builder.AddSeperator("<#50E451>Easiest Level</color>")
                    .AddBreakSpace()
                    .AddLine($"<#64D2FF>{easiestLevel.Name}</color> by <#FFD700>{easiestLevel.Author}</color>")
                    .AddBreakSpace()
                    .AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{easiestLevel.Attempt}</color>")
                    .AddBreakSpace()
                    .AddKeyValue("<#7FDBFF>PlayDuration</color>", $"<#FFFFFF>{easiestLevel.GetPlayDuration().ToFormattedString()}</color>")
                    .AddBreakSpace();
            }
        }
        catch (Exception e)
        {
            Logger.LogError(e);
            // Handle the exception or continue execution
        }

        // Add the "liked author" section if applicable
        (string Author, List<Level> Levels) likedAuthor = YouLikedThisAuthorALot();
        if (likedAuthor.Levels is { Count: > 0 })
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
            .AddKeyValue("<#7FDBFF>Status</color>", $"<#FFFFFF>{level.StatusString}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>PlayDuration</color>", $"<#FF7A7A>{level.GetPlayDuration().ToFormattedString()}</color>")
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
    ///     Formats the message shown when a level is broken
    /// </summary>
    public string MessageBrokenLevel(OnlineZeeplevel level)
    {
        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddSeperator("<#FF0000>Broken Level</color>")
            .AddBreakSpace()
            .AddLine("<#FF5555>Oops! This level appears to be broken or unplayable.</color>")
            .AddBreakSpace()
            .AddLine($"<#64D2FF>{level.Name}</color> by <#FFD700>{level.Author}</color>")
            .AddBreakSpace()
            .AddLine("<#FFFFFF>Automatic recovery in progress</color>")
            .AddBreakSpace()
            .AddLine($"<#AAAAAA>Retries left: <#FFFF00>{Retries}</color></color>")
            .AddBreakSpace()
            .AddLine("<#AAAAAA>No time penalty will be applied for this broken level</color>");

        return message.Build().ToString();
    }

    /// <summary>
    ///     Formats the message shown during level loading
    /// </summary>
    public string MessageLevelSummary()
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

        string diffColor = $"#{(result <= 0 ? ColorDefinitions.GreenSplit.CTToHexRGB() : ColorDefinitions.YellowSplit.CTToHexRGB())}"; // Green if better, red if worse
        string statusColor = CurrentLevel.AuthorTimeAcquired ? "#e600e6" :
            CurrentLevel.LevelBroken ? "#999999" :
            CurrentLevel.GoldMedalAcquired ? "#FFD600" :
            CurrentLevel.FreeSkipped ? "#00ffff" : "#bf3939";

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"<#64D2FF>{CurrentLevel.Name}</color> by <#FFD700>{CurrentLevel.Author}</color>");
        if (!Plugin.Instance.MyConfig.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddSeperator("<#B336A3>Result</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Status</color>", $"<{statusColor}>{CurrentLevel.StatusString}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Penalty</color>",
                    $"{(CurrentLevel.Status != Level.LevelStatus.FAILED ? "<#42b336>none</color>" : IsTimeOver() ? "<#0f0f0f>End of Run</color>" : $"<#FF5A5A>{PenaltyTimeInMilliseconds / 60 / 1000} minutes</color>")}");
        }

        message
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Stats</color>");
        if (!Plugin.Instance.MyConfig.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorDefinitions.Author.CTToHexRGB()}>AT</color>", $"<#FFFFFF>{CurrentLevel.AuthorTime.GetFormattedTime()}</color>")
                .AddBreakSpace()
                .AddKeyValue("<#7FDBFF>Your Time</color>", $"<#FFFFFF>{resultDisplay}</color>");
        }

        message
            .AddBreakSpace()
            .AddKeyValue($"{(CurrentLevel.AuthorTimeAcquired ? "<#50E451>Beaten by</color>" : $"<#{ColorDefinitions.YellowSplit.CTToHexRGB()}>Missed by</color>")}", $"<{diffColor}>{diffDisplay}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Attempts</color>", $"<#FFFFFF>{CurrentLevel.Attempt}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>PlayDuration</color>", $"<#FFFFFF>{CurrentLevel.GetPlayDuration().ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Time Wasted</color>", $"<#FF5A5A>{CurrentLevel.TimeWasted.ToFormattedString()}</color>")
            .AddBreakSpace()
            .AddSeperator("<#B336A3>Current Run</color>")
            .AddBreakSpace()
            .AddKeyValue("<#7FDBFF>Total ATs</color>", $"<#{ColorDefinitions.Author.CTToHexRGB()}>{AuthorMedals}</color>")
            .AddBreakSpace();

        // Use color based on remaining time
        string timeLeftColor = GetRemainingTime().TotalSeconds > 300 ? "#42b336" : // Green if > 5 minutes
            GetRemainingTime().TotalSeconds > 120 ? "#b3b300" : // Yellow if > 2 minutes
            "#bf3939"; // Red if < 2 minutes

        message.AddKeyValue("<#7FDBFF>Time left</color>", $"<{timeLeftColor}>{TimeFormatter.FormatDuration((int)GetRemainingTime().TotalMilliseconds)}</color>");
        return message.Build().ToString();
    }

    #endregion
}