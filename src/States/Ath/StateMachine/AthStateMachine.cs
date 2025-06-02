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
            State = paused ? "#999999" : "#42b336",
            TimeLeft = paused ? "#999999" : Ctx.IsTimeRunningLow ? "#bf3939" : Ctx.IsTimeAfterSkipRunningLow ? "#b3b300" : "#42b336",
            Author = ColorDefinitions.Author.CTToHexRGB(),
            Default = "#e6e6e6",
            AuthorSkip = "#e600e6",
            GoldSkip = "#FFD600",
            FreeSkip = "#00ffff",
            EndRunSkip = "#0f0f0f",
            PenaltySkip = "#bf3939",
            Section = "#ffd4a6"
        };

        string skipText = Ctx.CurrentLevel.AuthorTimeAcquired ? $"<{colors.AuthorSkip}>Author Skip" :
            Ctx.CurrentLevel.GoldMedalAcquired ? $"<{colors.GoldSkip}>Gold Skip" :
            Ctx.AvaiableFreeSkips > 0 ? $"<{colors.FreeSkip}>Free Skip ({Ctx.AvaiableFreeSkips}x left)" :
            Ctx.IsTimeRunningLow ? $"<{colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\">" :
            $"<{colors.PenaltySkip}>Penalty Skip!";

        string punishmentText = Ctx.Penalties == 0
            ? ""
            : $"(<{colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - <#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds * Ctx.Penalties).ToFormattedString()}</color>)";

        // string message = $"/servermessage white 0 " +
        string message =
            $"<size=\"20%\"><align=\"left\"><b><#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" +
            $"<{colors.Section}><b>=== Run Settings ===</b><br>" +
            $"<{colors.Default}>Duration      : {TimeSpan.FromMilliseconds(Ctx.Duration).ToFormattedString()}<br>" +
            $"<{colors.Default}>Skip Penalty  : <{colors.PenaltySkip}>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds).ToFormattedString()}</color><br>" +
            $"<{colors.Section}><b>=== Current Run ===</b><br>" +
            $"<{colors.Default}>State         : <{colors.State}>{(paused ? "PAUSED" : "ACTIVE")}<br>" +
            $"<{colors.Default}>Time Left     : <{colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTime().TotalMilliseconds)}</color> {punishmentText}<br>" +
            $"<{colors.Default}>AT/Gold/Skips : <{colors.AuthorSkip}>{Ctx.AuthorMedals}<{colors.Default}>/<{colors.GoldSkip}>{Ctx.GoldMedals}<{colors.Default}>/<{colors.PenaltySkip}>{Ctx.Penalties}<br>" +
            $"<{colors.Section}><b>=== Current Level ===</b><br>" +
            $"<{colors.Default}>Level Time    : <{colors.State}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)}<br>" +
            $"<{colors.Default}>Skip Type     : {skipText}<br>" +
            $"<{colors.Default}>Attempt       : {Ctx.CurrentLevel.Attempt}<br>";

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