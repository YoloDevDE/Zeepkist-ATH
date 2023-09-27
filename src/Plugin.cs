using AuthorTimeHunting.Commands;
using BepInEx;
using HarmonyLib;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting;

[BepInPlugin(MyPluginInfo.PLUGIN_GUID, MyPluginInfo.PLUGIN_NAME, MyPluginInfo.PLUGIN_VERSION)]
[BepInDependency("ZeepSDK")]
public class Plugin : BaseUnityPlugin
{
    private Harmony harmony;
    public StateManager StateManager;

    private void Awake()
    {
        harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
        harmony.PatchAll();
        ChatCommandApi.RegisterLocalChatCommand<StartChallengeCommand>();
        ChatCommandApi.RegisterLocalChatCommand<StopChallengeCommand>();
        ChatCommandApi.RegisterLocalChatCommand<SkipLevelCommand>();
        ChatCommandApi.RegisterLocalChatCommand<SkipBrokenLevelCommand>();
        StateManager = new StateManager();

        StartChallengeCommand.OnHandle += StateManager.StartChallenge;
        StopChallengeCommand.OnHandle += StateManager.StopChallenge;

        // Plugin startup logic
        Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    }


    private void OnDestroy()
    {
        harmony?.UnpatchSelf();
        harmony = null;
    }


    // private void Awake()
    // {
    //     harmony = new Harmony(MyPluginInfo.PLUGIN_GUID);
    //     harmony.PatchAll();
    //
    //
    //     ChatCommandApi.RegisterLocalChatCommand<StartAuthorHunt>();
    //     ChatCommandApi.RegisterLocalChatCommand<SkipLevel>();
    //     ChatCommandApi.RegisterLocalChatCommand<StopChallengeCommand>();
    //     ChatCommandApi.RegisterLocalChatCommand<SkipBrokenLevelCommand>();
    //     
    //     RacingApi.CrossedFinishLine += time => LevelBeaten(time);
    //     RacingApi.LevelLoaded += () => OnLevelLoaded();
    //     MultiplayerApi.DisconnectedFromGame += () => StopChallenge();
    //     ResetAll();
    //     // Config.Bind("Settings", "Default Duration", 60, "Sets the Default Duration of the ATH mod.");
    //
    //     // Plugin startup logic
    //     Logger.LogInfo($"Plugin {MyPluginInfo.PLUGIN_GUID} is loaded!");
    // }
    //
    //
    // private void Update()
    // {
    //     if (isRunning && !isLoading)
    //
    //         if (endTime <= DateTime.Now)
    //         {
    //             ChatApi.SendMessage("Time over!");
    //             StopChallenge();
    //         }
    // }
    //
    // private static void skip()
    // {
    //     ChatApi.SendMessage("/fs");
    //     ChatApi.ClearChat();
    //     isLoading = true;
    // }
    //
    // private static void LevelBeaten(float time)
    // {
    //     var checkpointsPassed = PlayerManager.Instance.currentMaster.playerResults.First().racepoints;
    //     var checkpointsTotal = PlayerManager.Instance.currentMaster.racePoints;
    //
    //     if (isRunning && checkpointsPassed == checkpointsTotal)
    //     {
    //         currentTime = DateTime.Now;
    //         var authorTime = PlayerManager.Instance.currentMaster.GlobalLevel.TimeAuthor;
    //         var goldTime = PlayerManager.Instance.currentMaster.GlobalLevel.TimeGold;
    //
    //         if (time <= authorTime)
    //         {
    //             ChatApi.ClearChat();
    //             var remainingTime = endTime - DateTime.Now;
    //             var remainingTimeStr = string.Format("{0:D2}m", remainingTime.TotalMinutes);
    //
    //             // Senden der separaten Befehle
    //             ChatApi.SendMessage($"/settime {(int)remainingTime.TotalSeconds}");
    //
    //
    //             ++levelsBeaten;
    //             isLoading = true;
    //             RacingApi.PlayerSpawned += skip;
    //         }
    //         else
    //         {
    //             if (time <= goldTime && !goldSkip)
    //             {
    //                 var messageBuilder = new MessageBuilder();
    //                 messageBuilder.AddSeparator()
    //                     .AddLine(" :party: Gold Skip unlocked! You can now bypass the level without a time penalty.")
    //                     .AddSeparator();
    //
    //                 var message = messageBuilder.Build();
    //                 ChatApi.SendMessage(message);
    //                 goldSkip = true;
    //             }
    //         }
    //     }
    // }
    //
    //
    // private static void OnLevelLoaded()
    // {
    //     isLoading = false;
    //     if (isRunning)
    //     {
    //         RacingApi.PlayerSpawned -= skip;
    //         goldSkip = false;
    //         var authorTime = PlayerManager.Instance.currentMaster.GlobalLevel.TimeAuthor;
    //         var goldTime = PlayerManager.Instance.currentMaster.GlobalLevel.TimeGold;
    //         authortimes.Add(authorTime);
    //         loadingTime = DateTime.Now.Subtract(currentTime);
    //         endTime = endTime.AddSeconds(loadingTime.TotalSeconds);
    //         var remainingTime = endTime - DateTime.Now;
    //
    //         // Senden der separaten Befehle
    //         ChatApi.SendMessage($"/settime {(int)remainingTime.TotalSeconds}");
    //
    //         new MessageBuilder()
    //             .AddLine(
    //                 $"{PlayerManager.Instance.currentMaster.GlobalLevel.Name} - {PlayerManager.Instance.currentMaster.GlobalLevel.Author}")
    //             .AddSeparator()
    //             .AddKeyValue("Authortime", authorTime.GetFormattedTime())
    //             .AddKeyValue("Goldtime", goldTime.GetFormattedTime())
    //             .AddSeparator()
    //             .BuildAndSend();
    //         MessengerApi.Log($"Loading time of {(int)loadingTime.TotalSeconds}s was added.", 5f);
    //     }
    // }
    //
    // private static void SkipNotBrokenLevel()
    // {
    //     var remainingTime = CalculateRemainingTime();
    //     var minimumTimeNeeded = TimeSpan.FromMinutes(penalty + 0.5);
    //     if (!isRunning)
    //     {
    //         MessengerApi.LogWarning("Challenge isn't running. Cannot skip level.");
    //         return;
    //     }
    //
    //
    //     if (remainingTime <= minimumTimeNeeded && !goldSkip)
    //     {
    //         MessengerApi.LogWarning("Skipping is not allowed. It would exceed the remaining challenge time.");
    //         return;
    //     }
    //
    //     var skipMessage = UpdateChallengeTimeAndGenerateSkipMessage();
    //     UpdateStatsAndNotify(skipMessage, remainingTime);
    // }
    //
    // private static TimeSpan CalculateRemainingTime()
    // {
    //     return endTime - DateTime.Now;
    // }
    //
    // private static string UpdateChallengeTimeAndGenerateSkipMessage()
    // {
    //     currentTime = DateTime.Now;
    //     string skipMessage;
    //     if (freeSkips > 0)
    //     {
    //         freeSkips = Math.Max(0, --freeSkips);
    //         skipMessage = "Level skipped! No penalty due to free skip!<br>Free Skips left : " + freeSkips;
    //     }
    //     else if (goldSkip)
    //     {
    //         skipMessage = "Level skipped! No penalty due to the Gold Skip!";
    //     }
    //     else
    //     {
    //         skipMessage = $"Level skipped! A time penalty of {penalty} minutes has been applied.";
    //         endTime = endTime.AddMinutes(-penalty);
    //         penaltyCount++;
    //     }
    //
    //     goldSkip = false;
    //     return skipMessage;
    // }
    //
    //
    // private static void UpdateStatsAndNotify(string skipMessage, TimeSpan remainingTime)
    // {
    //     remainingTime = CalculateRemainingTime();
    //     var remainingTimeStr = FormatTimeSpan(remainingTime);
    //
    //     // Senden der separaten Befehle
    //     ChatApi.SendMessage($"/settime {(int)remainingTime.TotalSeconds}");
    //     // Erstellen der Nachricht mit dem MessageBuilder
    //     var messageBuilder = new MessageBuilder();
    //     messageBuilder.AddSeparator()
    //         .AddLine(skipMessage)
    //         .AddSeparator()
    //         .AddKeyValue("Levels beaten", $"{levelsBeaten}")
    //         .AddKeyValue("Levels skipped", $"{++levelsSkipped}")
    //         .AddKeyValue("Levels broken", $"{levelsBroken}")
    //         .AddSeparator()
    //         .AddKeyValue("Time left", remainingTimeStr);
    //
    //     var message = messageBuilder.Build();
    //     ChatApi.SendMessage(message);
    //     skip();
    // }
    //
    // private static string FormatTimeSpan(TimeSpan timeSpan)
    // {
    //     return string.Format("{0:D2}:{1:D2}:{2:D2}", timeSpan.Hours, timeSpan.Minutes, timeSpan.Seconds);
    // }
    //
    //
    // private static void SkipBrokenLevel()
    // {
    //     if (isRunning)
    //     {
    //         if (authortimes.Count > 0) authortimes.Remove(authortimes.Count);
    //
    //         // Berechne die verbleibende Zeit
    //         var remainingTime = endTime - DateTime.Now;
    //         // Konvertiere verbleibende Zeit zu einem lesbaren String-Format
    //         var remainingTimeStr = string.Format("{0:D2}:{1:D2}:{2:D2}",
    //             remainingTime.Hours,
    //             remainingTime.Minutes,
    //             remainingTime.Seconds);
    //
    //         // Senden des separaten Befehls
    //         ChatApi.SendMessage("/fs");
    //
    //         var messageBuilder = new MessageBuilder();
    //         messageBuilder.AddSeparator()
    //             .AddLine("Level skipped cause it's broken")
    //             .AddSeparator()
    //             .AddKeyValue("Levels beaten", $"{levelsBeaten}")
    //             .AddKeyValue("Levels skipped", $"{levelsSkipped}")
    //             .AddKeyValue("Levels broken", $"{++levelsBroken}")
    //             .AddSeparator()
    //             .AddKeyValue("Time left", remainingTimeStr);
    //
    //         var message = messageBuilder.Build();
    //         ChatApi.SendMessage(message);
    //     }
    // }
    //
    // private static void SetTime(string timeStr)
    // {
    //     const int minTime = 300; // Minimumzeit in Sekunden (5 Minuten)
    //     const int maxTime = 86400; // Maximale Zeit in Sekunden (24 Stunden)
    //
    //     // Standardzeit setzen, wenn keine Zeitangabe vorhanden ist
    //     if (string.IsNullOrEmpty(timeStr)) timeStr = "3600"; // 60 Minuten als Standardwert
    //
    //     if (int.TryParse(timeStr, out var timeInSeconds))
    //     {
    //         if (timeInSeconds < minTime)
    //         {
    //             MessengerApi.LogWarning(
    //                 $"The provided time is too short. Setting the time to the minimum allowed value of {minTime} seconds (5 minutes).");
    //             timeInSeconds = minTime;
    //         }
    //         else if (timeInSeconds > maxTime)
    //         {
    //             MessengerApi.LogWarning(
    //                 $"The provided time exceeds the maximum allowed value. Setting the time to {maxTime} seconds (24 hours).");
    //             timeInSeconds = maxTime;
    //         }
    //
    //         endTime = DateTime.Now.AddSeconds(timeInSeconds);
    //     }
    //     else
    //     {
    //         MessengerApi.LogWarning("Invalid time format. Please provide the time duration in seconds.");
    //     }
    // }
    //
    //
    // private static void StartChallenge(string arguments)
    // {
    //     if (isRunning)
    //     {
    //         MessengerApi.LogWarning(
    //             "A challenge is already in progress. Use the /ath stop command to stop the current challenge before starting a new one.");
    //     }
    //     else
    //     {
    //         isRunning = true;
    //         startTime = DateTime.Now;
    //         SetTime(arguments); // Initialize the challenge with a duration of 60 minutes if not specified
    //
    //         // Calculate remaining time
    //         var remainingTime = endTime - DateTime.Now;
    //         currentTime = DateTime.Now;
    //         // Convert remaining time to a readable string format
    //         var remainingTimeStr = $"{remainingTime:hh\\:mm\\:ss}";
    //
    //         // Notify the chat about the challenge settings
    //         ChatApi.SendMessage($"/settime {(int)remainingTime.TotalSeconds}");
    //         // ChatApi.SendMessage("/fs");
    //         var messageBuilder = new MessageBuilder();
    //         messageBuilder.AddLine("Challenge has started!")
    //             .AddSeparator()
    //             .AddKeyValue("Target Medal", LevelModeBase.MedalType.Author.ToString())
    //             .AddKeyValue("Skip-Penalty", $"{penalty} Minutes")
    //             .AddKeyValue("Time", remainingTimeStr)
    //             .AddKeyValue("Free Skips", $"{freeSkips}")
    //             .AddSeparator();
    //
    //         var message = messageBuilder.Build();
    //         ChatApi.SendMessage(message);
    //         MessengerApi.LogSuccess("AT Hunt started");
    //
    //         skip();
    //     }
    // }
    //
    // private static void StopChallenge()
    // {
    //     if (!isRunning)
    //         return;
    //
    //     // Berechnen der AT (Aktionspunkte) pro Minute
    //     var atPerMinute = CalculateAtPerMinute();
    //
    //     // Anzeigen der Challenge-Ergebnisse
    //     DisplayChallengeResults(atPerMinute);
    //
    //     // Zurücksetzen aller Challenge-Variablen
    //     ResetAll();
    //     MessengerApi.LogSuccess("AT Hunt stopped");
    // }
    //
    // private static double CalculateAtPerMinute()
    // {
    //     var challengeDuration = currentTime - startTime;
    //     var minutesElapsed = challengeDuration.TotalMinutes;
    //     return levelsBeaten == 0 ? 0 : levelsBeaten / minutesElapsed;
    // }
    //
    // private static void DisplayChallengeResults(double atPerMinute)
    // {
    //     // Senden des separaten Befehls
    //     ChatApi.SendMessage("/settime 600");
    //     var avgAuthortime = 0.0;
    //     if (authortimes.Count > 0)
    //     {
    //         foreach (var authortime in authortimes) avgAuthortime += authortime;
    //
    //         avgAuthortime = avgAuthortime / authortimes.Count;
    //     }
    //
    //     var messageBuilder = new MessageBuilder();
    //     messageBuilder.AddSeparator()
    //         .AddLine("Challenge stopped! Results:")
    //         .AddSeparator()
    //         .AddKeyValue("Levels skipped", $"{levelsSkipped}")
    //         .AddKeyValue("Levels broken", $"{levelsBroken}")
    //         .AddSeparator()
    //         .AddKeyValue("ATs total", $"{levelsBeaten}")
    //         .AddKeyValue("ATs per Minute", $"{atPerMinute.ToString("F2", new CultureInfo("en-US"))} ATs / 1 min")
    //         .AddKeyValue("Average AT per Level", $"{avgAuthortime.ToString("F2", new CultureInfo("en-US"))}s / 1 level")
    //         .AddSeparator();
    //
    //     var message = messageBuilder.Build();
    //     ChatApi.SendMessage(message);
    // }
    //
    //
    // private class StartAuthorHunt : ILocalChatCommand
    // {
    //     public string Prefix => "/";
    //     public string Command => "ath start";
    //
    //     public string Description =>
    //         "Starts the AT Hunt. You can adjust the challenge-duration by adding the time in seconds for example: /ath start 600";
    //
    //     public void Handle(string arguments)
    //     {
    //         StartChallenge(arguments);
    //     }
    // }
    //
    // private class SkipLevel : ILocalChatCommand
    // {
    //     public string Prefix => "/";
    //     public string Command => "skip";
    //
    //     public string Description =>
    //         "Skips the current level if the challenge is active. depending on the circumstances you get a skip with a time penalty. Check the official rules for clarification.";
    //
    //     public void Handle(string arguments)
    //     {
    //         SkipNotBrokenLevel();
    //     }
    // }
    //
    // private class SkipBrokenLevelCommand : ILocalChatCommand
    // {
    //     public string Prefix => "/";
    //     public string Command => "broken";
    //
    //     public string Description =>
    //         "Whenever the track you are currently playing on is broken or you'd skipped to level 05 than use this command. it will skip to the next level without a penalty.";
    //
    //     public void Handle(string arguments)
    //     {
    //         SkipBrokenLevel();
    //     }
    // }
}