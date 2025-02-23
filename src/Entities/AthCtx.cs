using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Util;
using UnityEngine;
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
    public int Duration { get; } = 60 * 60;
    public int LoadingTimeInSeconds { get; set; } = 0;

    public int PauseTimeInSeconds { get; set; } = 0;

    public int PunishTime { get; } = 60 * 5;


    public int Punishments { set; get; } = 0;
    public int FreeSkips { get; set; } = 1;

    public bool TimeIsRunningLow { get; set; } = false;
    public Level CurrentLevel { get; set; }
    public LevelItem PreCachedLevel { get; set; }
    public List<Level> Levels { get; set; } = new List<Level>();


    public TimeSpan CurrentDuration => DateTime.Now.Subtract(EndTime).Duration();
    public int AuthorMedals { get; set; } = 0;
    public int GoldMedals { get; set; } = 0;

    public DateTime EndTime =>
        StartTime
            .AddSeconds(Duration + 1)
            .AddSeconds(PauseTimeInSeconds)
            .AddSeconds(LoadingTimeInSeconds)
            .AddSeconds(BrokenTimeInSeconds)
            .AddSeconds(-(PunishTime * Punishments));

    public int BrokenTimeInSeconds { get; set; } = 0;


    public IEnumerator MessageStartingCoroutine()
    {
        // Initialize a single message builder
        Message.Builder message = new Message.Builder();

        // Initial Message
        message
            .ClearLines()
            .AddLine("<size=100%><b><color=#FFA500>Author Time Hunt initiated</color></b>")
            .AddBreakSpace();

        if (!Plugin.Minimalist.Value)
        {
            // Add Game Objective to the ongoing message
            message
                .AddSeperator("<size=110%><b>Objective</b></size>")
                .AddBreakSpace()
                .AddLine($"Earn as many <color=#FFD700><b>Author Medals</b></color> as possible in <b>{TimeSpan.FromSeconds(Duration).ToFormattedString()}</b>.");
            Messenger.SendChat(message.Build().ToString());

            // Wait only if TutorialConfig is disabled
            if (Plugin.TutorialConfig.Value)
            {
                yield return new WaitForSeconds(5);
            }

            // Add Game Rules to the ongoing message
            message
                .AddBreakSpace()
                .AddSeperator("<size=110%><b>Rules</b></size>")
                .AddBreakSpace()
                .AddLine("<b>#1 Skipping</b>:<br><margin-left=3em><color=#ff8800>/fs</color> adds a penalty unless an <color=#AF00AF>Author</color> or <color=#FFD600>Gold</color> medal is achieved.</margin>")
                .AddBreakSpace()
                .AddLine("<b>#2 Broken Levels</b>:<br><margin-left=3em>If a level is unbeatable, use <color=#ff8800>/ath broken</color>.</margin>")
                .AddBreakSpace()
                .AddLine("<size=80%><color=#FF0000><b>Do not misuse commands.</b></color></size>");
            Messenger.SendChat(message.Build().ToString());

            // Wait only if TutorialConfig is disabled
            if (Plugin.TutorialConfig.Value)
            {
                yield return new WaitForSeconds(5);
            }
        }

        // Add Command List to the ongoing message
        message
            .AddBreakSpace()
            .AddSeperator("<size=110%><color=#1E90FF><b>Commands</b></color></size>")
            .AddBreakSpace()
            .AddLine("<size=90%>")
            .AddLine("<color=#00FF00>/fs</color> -> Skip the current level (penalty applied if no Author/Gold)").AddBreakSpace()
            .AddLine("<color=#00FF00>/ath broken</color> -> Skips a broken level").AddBreakSpace()
            .AddLine("<color=#00FF00>/ath restart</color> -> Start a new run").AddBreakSpace()
            .AddLine("<color=#00FF00>/ath stop</color> -> End the current run")
            .AddLine("</size>");
        Messenger.SendChat(message.Build().ToString());

        // Wait only if TutorialConfig is disabled
        if (Plugin.TutorialConfig.Value)
        {
            yield return new WaitForSeconds(5);
        }

        // Add final message and note about disabling tutorial without additional waiting
        message
            .AddBreakSpace()
            .AddLine("<size=100%><color=#00FF00>Good luck and have fun!</color></size>")
            .AddBreakSpace()
            .AddLine("<size=90%><color=#808080>Tip: You can disable this tutorial in the settings.</color></size>");
        Messenger.SendChat(message.Build().ToString());
    }

    public int CountTotalAttempts()
    {
        return Levels.Sum(level => level.Attempt);
    }

    public Level LevelYouShouldHaveSkippedThis()
    {
        return Levels
            .Where(level => level.Duration.TotalMinutes >= 5 && !level.LevelBroken)
            .OrderByDescending(level => level.Duration)
            .FirstOrDefault();
    }

    public (string Author, List<Level> Levels) YouLikedThisAuthorALot()
    {
        (string Author, List<Level> Levels) likedAuthor = Levels
            .Where(level => level.Levelbeaten) // Only consider beaten levels
            .GroupBy(level => level.Author) // Group by author
            .Where(group => group.Count() >= 2) // Find authors with 2 or more beaten levels
            .Select(group => (
                Author: group.Key,
                Levels: group.ToList() // Get the list of levels beaten by this author
            ))
            .FirstOrDefault(); // Take the first author that meets the condition

        return likedAuthor; // Will return null if no author meets the criteria
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

    public double AverageAttemptsPerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.Levelbeaten);

        if (!beatenLevels.Any())
        {
            return 0; // Return 0 or another appropriate default value if no levels have been beaten
        }

        return beatenLevels.Average(level => level.Attempt);
    }

    public TimeSpan AverageTimePerAt()
    {
        IEnumerable<Level> beatenLevels = Levels.Where(level => level.Levelbeaten);

        if (!beatenLevels.Any())
        {
            return TimeSpan.Zero; // Return zero if no levels have been beaten
        }

        // Calculate the average TimeSpan by converting to ticks
        long averageTicks = (long)beatenLevels.Average(level => level.Duration.Ticks);

        // Convert the average ticks back to TimeSpan
        return TimeSpan.FromTicks(averageTicks);
    }

    public string MessageRunning()
    {
        PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;

        // Initialize default values
        double result = 0;
        double positiveResult = 0;
        string diffDisplay = "--:--.---";
        string resultDisplay = "--:--.---";


        Message.Builder message = new Message.Builder()
            .ClearLines()
            .AddLine($"<color=#ff8800>{CurrentLevel.Name}</color> by <color=#ff8800>{CurrentLevel.Author}</color>")
            .AddBreakSpace()
            .AddSeperator("Goals")
            .AddBreakSpace()
            .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}");
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue("Gold", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
        }

        if (currentResult != null)
        {
            result = currentResult.Time - CurrentLevel.AuthorTime;
            positiveResult = Math.Abs(result);
            diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";

            message
                .AddBreakSpace()
                .AddKeyValue($"{(CurrentLevel.Levelbeaten ? (CurrentLevel.GoldSkipUnlocked ? "" : "AT ") + "Beaten by" : (CurrentLevel.GoldSkipUnlocked ? "" : "AT ") + "Missed by")}", diffDisplay);
        }

        if (CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue("Gold", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
        }

        return
            message
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

        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
            .AddBreakSpace()
            .AddSeperator("Result")
            .AddBreakSpace()
            .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}");
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue("Gold", $"{CurrentLevel.GoldTime.GetFormattedTime()}")
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
                .AddKeyValue("Gold", $"{CurrentLevel.GoldTime.GetFormattedTime()}")
                ;
        }

        return message
            .Build()
            .ToString();
    }

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
            .AddKeyValue("ATs Oneshot", $"{CountOneShotATs()}")
            .AddBreakSpace()
            .AddKeyValue("Attempts/AT", $"{AverageAttemptsPerAt():F2}")
            .AddBreakSpace()
            .AddKeyValue("Time/AT", $"{AverageTimePerAt().ToFormattedString()}")
            .AddBreakSpace();

        try
        {
            // Conditionally add the section for the longest duration level
            if (youShouldHaveSkippedThis != null)
            {
                builder.AddSeperator("You should have skipped this :yannics:")
                    .AddBreakSpace()
                    .AddLine($"{youShouldHaveSkippedThis.Name} by {youShouldHaveSkippedThis.Author}")
                    .AddBreakSpace()
                    .AddKeyValue("Status", $"{youShouldHaveSkippedThis.Status}")
                    .AddBreakSpace()
                    .AddKeyValue("Duration", $"{youShouldHaveSkippedThis.Duration.ToFormattedString()}")
                    .AddBreakSpace()
                    .AddKeyValue("Attempts", $"{youShouldHaveSkippedThis.Attempt}")
                    .AddBreakSpace();
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            // Handle the exception or continue execution
        }

        // Conditionally add the section for the author with beaten levels
        (string Author, List<Level> Levels) likedAuthor = YouLikedThisAuthorALot();
        if (likedAuthor.Levels != null && likedAuthor.Levels.Count > 0)
        {
            builder.AddSeperator("This Author haunted you :skull:")
                .AddBreakSpace()
                .AddLine($"And their name is ...<br><color=#ff8800>{likedAuthor.Author}</color>")
                .AddBreakSpace()
                .AddLine($"You've beaten {likedAuthor.Levels.Count} of their levels:")
                .AddBreakSpace();

            foreach (Level level in likedAuthor.Levels)
            {
                builder.AddLine($"- {level.Name}")
                    .AddBreakSpace();
            }
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
                .AddKeyValue("Penalty", $"{(CurrentLevel.Levelbeaten || CurrentLevel.LevelBroken || CurrentLevel.GoldSkipUnlocked || CurrentLevel.FreeSkipped ? "0 minutes" : $"{PunishTime / 60} minutes")}")
                ;
        }

        message
            .AddBreakSpace()
            .AddSeperator("Stats");
        if (!Plugin.Minimalist.Value)
        {
            message
                .AddBreakSpace()
                .AddKeyValue("AT", $"{CurrentLevel.AuthorTime.GetFormattedTime()}")
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


    public void ShowMessageRunningUI()
    {
        // UIBuilder uiBuilder = UIBuilder.Create(
        //     new Vector2(100, 100),
        //     new Vector2(400, 300),
        //     "Level Info",
        //     new Color(1f, 0.788f, 0.502f, 1f) // Zeepkist beige
        // );
        //
        // PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer.CurrentResult;
        // double result = 0;
        // double positiveResult = 0;
        // string diffDisplay = "--:--.---";
        //
        // uiBuilder.AddTMPLabel($"<color=#ff8800>{CurrentLevel.Name}</color> by <color=#ff8800>{CurrentLevel.Author}</color>")
        //     .AddSpace(10)
        //     .AddHorizontalLine()
        //     .AddTMPLabel("Goals")
        //     .AddSpace(10)
        //     .AddTMPLabel($"AT: {CurrentLevel.AuthorTime.GetFormattedTime()}");
        //
        // if (!CurrentLevel.GoldSkipUnlocked)
        // {
        //     uiBuilder.AddTMPLabel($"Gold: {CurrentLevel.GoldTime.GetFormattedTime()}");
        // }
        //
        // if (currentResult != null)
        // {
        //     result = currentResult.Time - CurrentLevel.AuthorTime;
        //     positiveResult = Math.Abs(result);
        //     diffDisplay = $"{StringUtils.GetSign(result)}{positiveResult.GetFormattedTime()}";
        //
        //     uiBuilder.AddSpace(10)
        //         .AddTMPLabel($"{(CurrentLevel.Levelbeaten ? "Beaten by" : "Missed by")}: {diffDisplay}");
        // }
        //
        // uiBuilder.AddButton("Close", () => { Plugin.Instance.MainGUI.ToggleVisibility(); });
        // Plugin.Instance.MainGUI.SetDynamicUI(uiBuilder);
    }

    public bool IsTimeOver()
    {
        return DateTime.Now >= EndTime;
    }
}