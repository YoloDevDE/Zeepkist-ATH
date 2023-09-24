using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Commands;
using UnityEngine;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class Challenge : MonoBehaviour
{
    public List<double> Authortimes = new();
    public int ChallengeDurationInMinutes = 60;

    public ChallengeState ChallengeState;
    public ChallengeStatePending ChallengeStatePending = new();
    public ChallengeStateRunning ChallengeStateRunning = new();
    public ChallengeStateStandby ChallengeStateStandby = new();
    public double CurrentAt;
    public double CurrentGold;
    public double CurrentPlayerFinish;
    public DateTime CurrentTime = new();

    public DateTime EndTime = new();
    public int FreeSkips = 1;

    public bool GoldSkip;
    public int LevelsBeaten = 0;
    public int LevelsBroken = 0;
    public int LevelsSkipped = 0;

    public TimeSpan LoadingTime = new();
    public DateTime LoadingTimeEnd = new();
    public DateTime LoadingTimeStart = new();
    public double Penalty = 5;
    public DateTime StartTime = new();


    public Challenge()
    {
        ChallengeState = new ChallengeStateRunning();
        ChallengeState.Enter(this);
        // RacingApi.LevelLoaded += test;
    }

    public void test()
    {
        ChatApi.SendMessage("o/");
    }

    public void SwitchState(ChallengeState challengeState)
    {
        ChallengeState = challengeState;
        challengeState.Enter(this);
    }

    public void OnLevelLoaded()
    {
        CurrentAt = PlayerManager.Instance.currentMaster.authorTime;
        CurrentGold = PlayerManager.Instance.currentMaster.goldTime;
        RunningMessage();
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

    public void SkipLevel()
    {
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

    public void CheckFinish(float time)
    {
        CurrentPlayerFinish = time;
        if (IsRunValid())
        {
            if (time <= CurrentAt)
            {
                PendingBeatenMessage();
                
                return;
            }

            if (time <= CurrentGold)
            {
                GoldSkip = true;
                ServerMessage();
            }
        }
    }

    public void PendingBeatenMessage()
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

    public bool IsRunValid()
    {
        var checkpointsPassed = PlayerManager.Instance.currentMaster.playerResults.First().racepoints;
        var checkpointsTotal = PlayerManager.Instance.currentMaster.racePoints;
        return checkpointsTotal == checkpointsPassed;
    }


    public void ServerMessage()
    {
        new MessageBuilder(autoBreak: false, startWithBreak: false)
            .AddKeyValue("Free Skips", $"{FreeSkips}")
            .AddLine("<br>")
            .AddKeyValue("Gold Skip ", $"{(GoldSkip ? "unlocked" : "locked")}")
            .BuildAndServermessage(GoldSkip ? "green" : "yellow");
    }

    public void StartChallenge()
    {
    }

    public void StopChallenge()
    {
    }

    public void ClearChat()
    {
        var msg = "";
        for (var i = 0; i < 40; i++) msg += "<br>";

        ChatApi.SendMessage(msg);
    }
}