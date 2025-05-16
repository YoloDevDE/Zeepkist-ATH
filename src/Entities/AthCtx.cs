using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Entities;

public class AthCtx
{
    public int Skips = 0;

    public AthCtx()
    {
        FetchNextLevel();
    }
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
    public LevelItem NextLevel { get; set; }
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


    public string MessageStarting()
    {
        Message.Builder message = new Message.Builder();
        message
            .ClearLines()
            .AddLine("<#FFD700>Welcome to Author-Time-Hunting!</color>")
            .AddBreakSpace();

        if (!Plugin.Minimalist.Value)
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
        else
        {
            message
                .AddSeperator("Commands")
                .AddBreakSpace()
                .AddKeyValue("/fs", "Skip level (free)")
                .AddKeyValue("/ath broken", "Skip unbeatable map")
                .AddKeyValue("/ath restart", "Restart the hunt")
                .AddKeyValue("/ath stop", "End the hunt");
        }

        message
            .AddBreakSpace()
            .AddLine("<color=#FFD700>Good luck & have fun!</color>");

        return message.Build().ToString();
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

    public async Task FetchNextLevel()
    {
        NextLevel = await GraphQLService.Instance.GetRandomLevelAsync();
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
        string diffDisplay = " --:--.---";
        string resultDisplay = " --:--.---";


        Message.Builder message = new Message.Builder()
            .ClearLines()
            .AddLine($"{CurrentLevel.Name} by {CurrentLevel.Author}")
            .AddBreakSpace()
            .AddSeperator("Goals")
            .AddBreakSpace()
            .AddKeyValue($"<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{CurrentLevel.AuthorTime.GetFormattedTime()}");
        if (!CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
        }

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

        if (CurrentLevel.GoldSkipUnlocked)
        {
            message
                .AddBreakSpace()
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}");
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
                .AddKeyValue($"<#{ColorExtension.bg_Gold.CTToHexRGB()}>Gold</color>", $"{CurrentLevel.GoldTime.GetFormattedTime()}")
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
            .AddKeyValue("ATs Oneshotted!", $"{CountOneShotATs()}")
            .AddBreakSpace()
            .AddKeyValue($"Attempts/<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{AverageAttemptsPerAt():F2}")
            .AddBreakSpace()
            .AddKeyValue($"Time/<#{ColorExtension.bg_Author.CTToHexRGB()}>AT</color>", $"{AverageTimePerAt().ToFormattedString()}")
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

    public bool IsTimeOver()
    {
        return DateTime.Now >= EndTime;
    }
}