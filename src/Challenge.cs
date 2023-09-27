using System;
using System.Collections.Generic;
using ZeepSDK.Chat;

namespace AuthorTimeHunting;

public class Challenge
{
    public List<double> Authortimes = new();
    public int ChallengeDurationInMinutes = 60;

    public double CurrentAt;
    public double CurrentGold;
    public double CurrentPlayerFinish;
    public DateTime CurrentTime = new();

    public DateTime EndTime;
    public int FreeSkips = 1;

    public bool GoldSkip;
    public int LevelsBeaten = 0;
    public int LevelsBroken = 0;
    public int LevelsSkipped = 0;

    public TimeSpan LoadingTime = new();
    public DateTime LoadingTimeEnd = new();
    public DateTime LoadingTimeStart = new();
    public double Penalty = 5;
    public DateTime StartTime;

    public Challenge()
    {
        ChallengeState = new StateStandby(this);
        ChallengeState.Enter();
    }

    public ChallengeState ChallengeState { get; private set; }

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
            .AddKeyValue("Time left", $"~{(EndTime - DateTime.Now).TotalMinutes:0}min")
            .Build();
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