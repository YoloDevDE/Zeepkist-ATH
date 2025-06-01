using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using Crosstales;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : IStateMachine
{
    private bool _timerStarted;

    public AthStateMachine()
    {
        Ctx = new AthCtx();
        Timer = new AthTimer();
        InitialState = new StateAthStarting(this);
        FinalState = new StateAthStopping(this);
        _timerStarted = false;
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


    public void SetServerMessage(bool paused)
    {
        var colors = new
        {
            State = paused ? "#ff8800" : "#00ff44",
            TimeLeft = paused ? "#ff8800" : Ctx.IsTimeRunningLow ? "#ff4a4a" : "#ffffff",
            CurrentLevel = paused ? "#ff8800" : "#ffffff",
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
            Ctx.IsTimeRunningLow ? $"<{colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\">" :
            $"<{colors.PenaltySkip}>Penalty Skip!";

        string punishmentText = Ctx.Punishments == 0
            ? ""
            : $"(<{colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - " +
              $"<#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PunishTimeInMilliseconds * Ctx.Punishments).ToFormattedString()}</color>)";

        // string message = $"/servermessage white 0 " +
        string message =
            $"<size=\"20%\"><align=\"left\"><b><#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" +

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

        // ChatApi.SendMessage(message);
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.serverMessageText.text = message;
    }

    public void OnAthTimerTick()
    {
        ((AthState)CurrentState).OnAthTimerTick();
    }

    public void StartTimer()
    {
        if (_timerStarted)
        {
            return;
        }

        Timer.Start();
        Timer.Tick += OnAthTimerTick;
        _timerStarted = true;
    }

    public void StopTimer()
    {
        Timer.Stop();

        Timer.Tick -= OnAthTimerTick;
        _timerStarted = false;
    }
}