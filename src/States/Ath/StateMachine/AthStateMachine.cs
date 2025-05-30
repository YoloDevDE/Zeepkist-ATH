using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : IStateMachine, IDisposable
{
    private bool disposed;
    private bool timerStarted;

    public AthStateMachine()
    {
        Ctx = new AthCtx();
        Timer = new AthTimer();
        InitialState = new StateAthStarting(this);
        FinalState = new StateAthStopping(this);
        timerStarted = false;
    }

    private AthTimer Timer { get; }
    public AthCtx Ctx { get; set; }


    public IState CurrentState { get; set; }
    public IState InitialState { get; }
    public IState FinalState { get; }
    public event Action StateMachineFinished;

    public void InvokeFinish()
    {
        StateMachineFinished?.Invoke();
    }

    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }


    public void SetServerMessage(bool paused)
    {
        var colors = new
        {
            State = paused ? "#ff8800" : "#00ff44",
            TimeLeft = paused ? "#ff8800" : Ctx.GetRemainingTime().TotalMilliseconds > Ctx.PunishTime ? "#00ff44" : "#ff4a4a",
            CurrentLevel = paused ? "#ff8800" : "#00ff44",
            Author = ColorDefinitions.Author.CTToHexRGB(),
            Default = "#ffffff",
            AuthorSkip = "#AF00AF",
            GoldSkip = "#FFD600",
            FreeSkip = "#00ffff",
            EndRunSkip = "#0f0f0f",
            PenaltySkip = "#FF0000",
            Section = "#64D2FF"
        };

        string skipText = Ctx.CurrentLevel.LevelBeaten ? $"<{colors.AuthorSkip}>Author Skip" :
            Ctx.CurrentLevel.GoldSkipUnlocked ? $"<{colors.GoldSkip}>Gold Skip" :
            Ctx.FreeSkips > 0 ? $"<{colors.FreeSkip}>Free Skip ({Ctx.FreeSkips}x left)" :
            Ctx.TimeIsRunningLow ? $"<{colors.EndRunSkip}>:skull:FATAL SKIP:skull:" :
            $"<{colors.PenaltySkip}>Penalty Skip!";

        string punishmentText = Ctx.Punishments == 0
            ? ""
            : $"(<{colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - " +
              $"<#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PunishTime * Ctx.Punishments).ToFormattedString()}</color>)";

        string message = $"/servermessage white 0 <size=\"20%\"><align=\"left\"><b><#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" +

                         // === RUN STATUS ===
                         $"<{colors.Section}>=== Run Status ===<br>" +
                         $"<{colors.Default}>State         : <{colors.State}>{(paused ? "PAUSE" : "ACTIVE")}<br>" +
                         $"<{colors.Default}>Time Left     : <{colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTime().TotalMilliseconds)}</color> {punishmentText}<br>" +
                         $"<{colors.Default}>Skip Type     : {skipText}<br>" +

                         // === CURRENT LEVEL ===
                         $"<{colors.Section}>=== Current Level ===<br>" +
                         $"<{colors.Default}>Level Time    : <{colors.CurrentLevel}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)}<br>" +
                         $"<{colors.Default}>Attempt       : {Ctx.CurrentLevel.Attempt}<br>" +

                         // === OVERALL STATS ===
                         $"<{colors.Section}>=== Overall Stats ===<br>" +
                         $"<{colors.Default}>AT/Gold/Skips : <{colors.AuthorSkip}>{Ctx.AuthorMedals}<{colors.Default}>/<{colors.GoldSkip}>{Ctx.GoldMedals}<{colors.Default}>/<{colors.PenaltySkip}>{Ctx.Skips - Ctx.GoldMedals}<br>";

        ChatApi.SendMessage(message);
    }

    public void OnAthTimerTick()
    {
        ((AthState)CurrentState).OnAthTimerTick();
    }

    public void StartTimer()
    {
        if (timerStarted)
        {
            return;
        }

        Timer.Start();
        Timer.Tick += OnAthTimerTick;
        timerStarted = true;
    }

    public void StopTimer()
    {
        Timer.Stop();

        Timer.Tick -= OnAthTimerTick;
        timerStarted = false;
    }

    protected virtual void Dispose(bool disposing)
    {
        if (disposed)
        {
            return;
        }

        if (disposing)
        {
            StopTimer();
            Timer?.Dispose();
        }

        disposed = true;
    }

    ~AthStateMachine()
    {
        Dispose(false);
    }
}