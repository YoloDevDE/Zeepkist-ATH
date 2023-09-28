using System;
using System.Collections.Generic;
using System.Globalization;
using ZeepSDK.Chat;

namespace AuthorTimeHunting;

public class Challenge
{
    public List<float> Authortimes = new();
    public int ChallengeDurationInMinutes = 60;
    public double CurrentAt;
    public double CurrentGold;
    public double CurrentPlayerFinish;
    public DateTime CurrentTime = new();

    public DateTime EndTime;
    public int FreeSkips = 1;

    public bool GoldSkip;
    public bool IsChallengeRunning = false;
    public int LevelsBeaten = 0;
    public int LevelsBroken = 0;
    public int LevelsSkipped = 0;

    public TimeSpan LoadingTime = new();
    public DateTime LoadingTimeEnd = new();
    public DateTime LoadingTimeStart = new();
    public double Penalty = 5;
    public DateTime StartTime;
    public ChallengeStateManager ChallengeStateManager;

    public Challenge(ChallengeStateManager challengeStateManager)
    {
        ChallengeState = new StateStandby(this);
        ChallengeStateManager = challengeStateManager;
        ChallengeState.Enter();
    }

    public ChallengeState ChallengeState { get; set; }

    public void SkipLevel(string message = "Stats")
    {
        new MessageBuilder()
            .ClearChat()
            .AddLine(message)
            .AddSeparator()
            .AddLine(stats())
            .BuildAndSend();
        ChatApi.SendMessage("/fs");
    }

    public string stats()
    {
        return new MessageBuilder(startWithBreak: false)
            .AddKeyValue("ATs gained", $"{LevelsBeaten}")
            .AddKeyValue("Skipped", $"{LevelsSkipped}")
            .AddKeyValue("Broken", $"{LevelsBroken}")
            .AddSeparator()
            .AddKeyValue("Free Skips", $"{FreeSkips}")
            .AddKeyValue("Time left",
                $"{(EndTime - DateTime.Now).Minutes:0}min {(EndTime - DateTime.Now).Seconds:0}s ")
            .Build();
    }

    public void endStats()
    {
        new MessageBuilder()
            .ClearChat()
            .AddSeparator()
            .AddLine("Final Results")
            .AddSeparator()
            .AddKeyValue("ATs gained", $"{LevelsBeaten}")
            .AddKeyValue("Skipped", $"{LevelsSkipped}")
            .AddKeyValue("Broken", $"{LevelsBroken}")
            .AddSeparator()
            .AddKeyValue("Minutes per AT",
                $"{(LevelsBeaten <= 0 ? 0 : (DateTime.Now.AddMinutes(-LoadingTime.TotalMinutes) - StartTime).TotalMinutes / (LevelsBeaten <= 0 ? 1 : LevelsBeaten)).ToString("F2", CultureInfo.InvariantCulture).TrimEnd('0').TrimEnd('.')}")
            .AddKeyValue("Avg AT length",
                $"{AvgAt().GetFormattedTimeNoMilliSeconds()}")
            .BuildAndSend();
    }

    public double AvgAt()
    {
        float tmp = 0;
        foreach (var authortime in Authortimes) tmp += authortime;

        return Authortimes.Count <= 0 ? 0 : tmp / Authortimes.Count;
    }

    public void SwitchState(ChallengeState newState)
    {
        ChallengeState.Exit();
        ChallengeState = newState;
        ChallengeState.Enter();
    }

    public void SetChallengeTime(int duration)
    {
        StartTime = DateTime.Now;
        EndTime = DateTime.Now.AddMinutes(duration);
        ChallengeDurationInMinutes = duration;
    }

    public string GoldSkipLockedOrUnlocked()
    {
        return GoldSkip ? "unlocked" : "locked";
    }
}