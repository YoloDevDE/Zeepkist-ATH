using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Commands;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.ChatCommands;
using ZeepSDK.Leaderboard;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class Challenge
{
    public List<double> Authortimes = new();
    public int ChallengeDurationInMinutes = 60;
    public DateTime CurrentTime = new();

    public DateTime EndTime = new();
    public int FreeSkips = 1;

    public bool GoldSkip = false;
    public int LevelsBeaten = 0;
    public int LevelsBroken = 0;
    public int LevelsSkipped = 0;

    public TimeSpan LoadingTime = new();
    public DateTime LoadingTimeEnd = new();
    public DateTime LoadingTimeStart = new();
    public double Penalty = 5;
    public double CurrentAt = 0;
    public double CurrentGold = 0;
    public double CurrentPlayerFinish = 0;
    public DateTime StartTime = new();
    public State State = State.Ready;

    public Challenge()
    {
        ChallengeStart.OnHandle += StartChallenge;
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    private void OnLevelLoaded()
    {
        CurrentAt = PlayerManager.Instance.currentMaster.authorTime;
        CurrentGold = PlayerManager.Instance.currentMaster.goldTime;
        if (State == State.Pending)
        {
            State = State.Running;
            RunningMessage();
        }
    }

    public void RunningMessage()
    {
        ClearChat();
        new MessageBuilder()
            .AddKeyValue("AT", $"{CurrentAt.GetFormattedTime()}")
            .AddKeyValue("Gold", $"{CurrentGold.GetFormattedTime()}")
            .BuildAndSend();
    }

    public Challenge Reset()
    {
        ChallengeStart.OnHandle -= StartChallenge;
        RacingApi.LevelLoaded -= ServerMessage;
        RacingApi.LevelLoaded -= OnLevelLoaded;
        return new Challenge();
    }

    private void SkipLevel()
    {


            RacingApi.PlayerSpawned -= SkipLevel;
        ChatApi.SendMessage("/fs");
        GoldSkip = false;
    }

    public void StartMessage()
    {
        
        ClearChat();
        new MessageBuilder()
            .AddLine("Author Time Hunt started")
            .AddSeparator()
            .AddKeyValue("Free Skips", $"{FreeSkips}")
            .AddKeyValue("Duration", $"{ChallengeDurationInMinutes} Minutes")
            .AddSeparator()
            .BuildAndSend();
    }

    private void CheckFinish(double time)
    {
        CurrentPlayerFinish = time;
        if (State == State.Running)
        {
            if (IsRunValid())
            {
                if (time <= CurrentAt)
                {
                    State = State.Pending;
                    RacingApi.PlayerSpawned += SkipLevel;
                    PendingBeatenMessage();
                    return;
                }

                if (time <= CurrentGold)
                {
                    GoldSkip = true;
                    ServerMessage();
                    return;
                }
            }
        }
    }

    private void PendingBeatenMessage()
    {
        ClearChat();
        new MessageBuilder()
            .AddKeyValue("AT", $"{CurrentAt.GetFormattedTime()}")
            .AddKeyValue("Your Time", $"{CurrentPlayerFinish.GetFormattedTime()}")
            .AddKeyValue("Difference", $"{(CurrentAt - CurrentPlayerFinish).GetFormattedTime()}")
            .AddSeparator()
            .AddLine("You can now Respawn to skip to the next Level")
            .BuildAndSend();
    }

    private bool IsRunValid()
    {
        var checkpointsPassed = PlayerManager.Instance.currentMaster.playerResults.First().racepoints;
        var checkpointsTotal = PlayerManager.Instance.currentMaster.racePoints;
        return checkpointsTotal == checkpointsPassed;
    }


    private void ServerMessage()
    {
        new MessageBuilder(autoBreak: false, startWithBreak: false)
            .AddKeyValue("Free Skips", $"{FreeSkips}")
            .AddLine("<br>")
            .AddKeyValue("Gold Skip ", $"{(GoldSkip ? "unlocked" : "locked")}")
            .BuildAndServermessage((GoldSkip ? "green" : "yellow"));
    }

    private void StartChallenge()
    {
        if (State == State.Ready)
        {
            State = State.Pending;
            RacingApi.LevelLoaded += ServerMessage;
            RacingApi.CrossedFinishLine += time => CheckFinish(time);
            StartMessage();
            MessengerApi.LogSuccess("ATH started");
            SkipLevel();
            return;
        }

        MessengerApi.LogWarning("ATH already running");
    }

    public void StopChallenge()
    {
    }

    public void ClearChat()
    {
        string msg = "";
        for (int i = 0; i < 40; i++)
        {
            msg += "<br>";
        }

        ChatApi.SendMessage(msg);
    }
}

public enum State
{
    Running,
    Ready,
    Pending
}