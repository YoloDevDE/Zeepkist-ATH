using System;
using System.Linq;
using System.Timers;
using AuthorTimeHunting.Commands;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class StateRunning : ChallengeState
{
    private readonly Challenge _challenge;
    private Timer _timer;

    public StateRunning(Challenge challenge) : base(challenge)
    {
        _challenge = challenge;
    }

    public override void Enter()
    {
        RacingApi.CrossedFinishLine += CheckFinishValidity;
        RacingApi.PlayerSpawned += OnRespawn;
        SkipLevelCommand.OnHandle += SkipLevelCommandOnOnHandle;
        SkipBrokenLevelCommand.OnHandle += SkipBrokenLevelCommandOnOnHandle;

        
        _timer = new Timer(500);
        _timer.Elapsed += SendChatMessage;
        _timer.AutoReset = true;
        _timer.Start();
    }

    private void SkipBrokenLevelCommandOnOnHandle()
    {
        _challenge.LevelsBroken++;
        _challenge.SkipLevel("Skipping broken level...");
        _challenge.SwitchState(new StateLoading(Challenge));
    }

    public override void Exit()
    {
        SkipLevelCommand.OnHandle -= SkipLevelCommandOnOnHandle;
        SkipBrokenLevelCommand.OnHandle -= SkipBrokenLevelCommandOnOnHandle;
        RacingApi.CrossedFinishLine -= CheckFinishValidity;
        RacingApi.PlayerSpawned -= OnRespawn;

        _timer.Stop();
        _timer.Elapsed -= SendChatMessage;
        _timer.Dispose();
    }

    private void SkipLevelCommandOnOnHandle()
    {
        string message = "";
        TimeSpan remainingTime = _challenge.EndTime - DateTime.Now;
        bool canSkip = true;

        if (_challenge.GoldSkip)
        {
            message = "Level skipped using a gold skip. No penalty time applied.";
        }
        else if (_challenge.FreeSkips >= 1)
        {
            _challenge.FreeSkips--;
            message = "Level skipped using a free skip. No penalty time applied.";
        }
        else if (remainingTime.TotalMinutes >= _challenge.Penalty)
        {
            _challenge.EndTime = _challenge.EndTime.AddMinutes(-_challenge.Penalty);
            message = "Level skipped. Penalty time applied.";
        }
        else
        {
            message = "Cannot skip level as the remaining time is less than the penalty time and no gold or free skips are available.";
            canSkip = false;
        }

        if (canSkip)
        {
            _challenge.LevelsSkipped++;
            _challenge.SkipLevel(message);
            _challenge.SwitchState(new StateLoading(_challenge));
        }
        else
        {
            // You can add a message or logic here to inform the user that skipping is not possible
            Console.WriteLine(message); // Example
        }
    }



    public void OnRespawn()
    {
        _challenge.CurrentAt = PlayerManager.Instance.currentMaster.authorTime;
        _challenge.CurrentGold = PlayerManager.Instance.currentMaster.goldTime;
        _challenge.CurrentGold = PlayerManager.Instance.currentMaster.goldTime;
        var levelName = PlayerManager.Instance.currentMaster.GlobalLevel.Name;
        var authorName = PlayerManager.Instance.currentMaster.GlobalLevel.Author;
        new MessageBuilder()
            .ClearChat()
            .AddLine($"{levelName} by {authorName}")
            .AddSeparator()
            .AddKeyValue("AT", $"{Challenge.CurrentAt.GetFormattedTime()}")
            .AddKeyValue("Gold", $"{Challenge.CurrentGold.GetFormattedTime()}")
            .AddSeparator()
            .AddKeyValue("Goldskip", Challenge.GoldSkipLockedOrUnlocked())
            .BuildAndSend();
    }

    private int i = 0;

    private void SendChatMessage(object sender, ElapsedEventArgs e)
    {
        i++;
        var elapsedTime = _challenge.EndTime - DateTime.Now;
        var formattedTime = elapsedTime.Hours > 0
            ? $"{elapsedTime.Hours:00}:{elapsedTime.Minutes:00}:{elapsedTime.Seconds:00}"
            : $"{elapsedTime.Minutes:00}:{elapsedTime.Seconds:00}";
        if (elapsedTime.TotalMinutes <= 0)
            _challenge.SwitchState(new StateEnding(_challenge));
        else if (elapsedTime.TotalSeconds <= 10 && i % 2 == 0)
            ChatApi.SendMessage($"/servermessage red 0 {formattedTime}");
        else if (elapsedTime.TotalSeconds <= 10 && i % 2 == 1)
            ChatApi.SendMessage($"/servermessage remove");
        // ChatApi.SendMessage($"/servermessage yellow 0 {formattedTime}");
        else if (elapsedTime.TotalMinutes < 5)
            ChatApi.SendMessage($"/servermessage red 0 {formattedTime}");
        else
            ChatApi.SendMessage($"/servermessage yellow 0 {formattedTime}");
    }

    public void CheckFinishValidity(float time)
    {
        var checkpointsPassed = PlayerManager.Instance.currentMaster.playerResults.First().racepoints;
        var checkpointsTotal = PlayerManager.Instance.currentMaster.racePoints;
        _challenge.CurrentAt = PlayerManager.Instance.currentMaster.authorTime;
        _challenge.CurrentGold = PlayerManager.Instance.currentMaster.goldTime;
        _challenge.CurrentPlayerFinish = time;
        if (checkpointsTotal == checkpointsPassed)
        {
            if (time <= Challenge.CurrentAt)
            {
                new MessageBuilder()
                    .ClearChat()
                    .AddLine("AT collected! :party:")
                    .AddSeparator()
                    .AddKeyValue("AT", $" {_challenge.CurrentAt.GetFormattedTime()}")
                    .AddKeyValue("Your Time", $" {_challenge.CurrentPlayerFinish.GetFormattedTime()}")
                    .AddKeyValue("Difference",
                        $"-{(_challenge.CurrentAt - _challenge.CurrentPlayerFinish).GetFormattedTime()}")
                    .AddSeparator()
                    .AddLine("You can now Respawn to skip to the next Level")
                    .BuildAndSend();
                _challenge.LevelsBeaten++;
                _challenge.SwitchState(new StateWon(_challenge));
                return;
            }

            if (time <= Challenge.CurrentGold)
            {
                new MessageBuilder()
                    .ClearChat()
                    .AddLine("Level in progress")
                    .AddSeparator()
                    .AddKeyValue("AT", $" {_challenge.CurrentAt.GetFormattedTime()}")
                    .AddKeyValue("Your Time", $" {_challenge.CurrentPlayerFinish.GetFormattedTime()}")
                    .AddKeyValue("Difference",
                        $"+{(_challenge.CurrentPlayerFinish - _challenge.CurrentAt).GetFormattedTime()}")
                    .AddSeparator()
                    .AddLine("Gold Skip unlocked!")
                    .BuildAndSend();
                Challenge.GoldSkip = true;
                return;
            }

            new MessageBuilder()
                .ClearChat()
                .AddLine("Level in progress")
                .AddSeparator()
                .AddKeyValue("AT", $" {_challenge.CurrentAt.GetFormattedTime()}")
                .AddKeyValue("Your Time", $" {_challenge.CurrentPlayerFinish.GetFormattedTime()}")
                .AddKeyValue("Difference",
                    $"+{(_challenge.CurrentPlayerFinish - _challenge.CurrentAt).GetFormattedTime()}")
                .AddSeparator()
                .BuildAndSend();
        }
    }
}