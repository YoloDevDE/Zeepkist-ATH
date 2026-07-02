using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using Crosstales;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : IStateMachine
{
    private static readonly TimeSpan ServerMessageThrottle = TimeSpan.FromMilliseconds(500);
    private bool _eventsSubscribed;
    private string _lastServerMessage;
    private DateTime _lastServerMessageTime = DateTime.MinValue;
    private bool _timerStarted;

    public AthStateMachine()
    {
        Ctx = new AthCtx();
        Timer = new AthTimer();
        InitialState = new StateAthStarting(this);
        FinalState = new StateAthStopping(this);
        _eventsSubscribed = false;
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
            State = paused ? "#999999" : "#42b336", TimeLeft = paused
                ? "#999999"
                : Ctx.IsTimeRunningLow
                    ? "#bf3939"
                    : Ctx.IsTimeAfterSkipRunningLow
                        ? "#b3b300"
                        : "#42b336"
            , Author = ColorDefinitions.Author.CTToHexRGB(), Default = "#e6e6e6", AuthorSkip = "#e600e6", GoldSkip = "#FFD600", FreeSkip = "#00ffff", EndRunSkip = "#0f0f0f", PenaltySkip = "#bf3939", Section = "#ffd4a6"
        };

        string skipText = Ctx.CurrentLevel.AuthorTimeAcquired
            ? $"<color={colors.AuthorSkip}>Author Skip</color>"
            : Ctx.CurrentLevel.GoldMedalAcquired
                ? $"<color={colors.GoldSkip}>Gold Skip</color>"
                : Ctx.AvaiableFreeSkips > 0
                    ? $"<color={colors.FreeSkip}>Free Skip ({Ctx.AvaiableFreeSkips}x left)</color>"
                    : Ctx.IsTimeRunningLow
                        ? $"<color={colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\"></color>"
                        : $"<color={colors.PenaltySkip}>Penalty Skip!</color>";

        string punishmentText = Ctx.Penalties == 0
            ? ""
            : $"(<color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - <color=#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds * Ctx.Penalties).ToFormattedString()}</color>)";

        // string message = $"/servermessage white 0 " +
        string message = $"<size=\"20%\"><align=left><b><color=#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" + $"<color={colors.Section}><b>=== Run Settings ===</b></color><br>" +
                         $"<color={colors.Default}>Duration      : {TimeSpan.FromMilliseconds(Ctx.Duration).ToFormattedString()}</color><br>" +
                         $"<color={colors.Default}>Skip Penalty  : <color={colors.PenaltySkip}>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds).ToFormattedString()}</color></color><br>" +
                         $"<color={colors.Section}><b>=== Current Run ===</b></color><br>" + $"<color={colors.Default}>State         : <color={colors.State}>{(paused ? "PAUSED" : "ACTIVE")}</color></color><br>" +
                         $"<color={colors.Default}>Time Left     : <color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTime().TotalMilliseconds)}</color> {punishmentText}</color><br>" +
                         $"<color={colors.Default}>AT/Gold/Skips : <color={colors.AuthorSkip}>{Ctx.AuthorMedals}</color><color={colors.Default}>/</color><color={colors.GoldSkip}>{Ctx.GoldMedals}</color><color={colors.Default}>/</color><color={colors.PenaltySkip}>{Ctx.Penalties}</color></color><br>" +
                         $"<color={colors.Section}><b>=== Current Level ===</b></color><br>" +
                         $"<color={colors.Default}>Level Time    : <color={colors.State}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)}</color></color><br>" +
                         $"<color={colors.Default}>Skip Type     : {skipText}</color><br>" + $"<color={colors.Default}>Attempt       : {Ctx.CurrentLevel.Attempt}</color><br>" + "</align></size>";

        // ChatApi.SendMessage(message);
        DateTime now = DateTime.UtcNow;

        if (message == _lastServerMessage && now - _lastServerMessageTime < ServerMessageThrottle)
        {
            return;
        }

        _lastServerMessage = message;
        _lastServerMessageTime = now;
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.serverMessageText.text = message;
    }

    public void OnAthTimerTick()
    {
        (CurrentState as AthState)?.OnAthTimerTick();
    }

    private void OnRoundStarted()
    {
        (CurrentState as AthState)?.OnRoundStarted();
    }

    private void OnRoundEnded()
    {
        (CurrentState as AthState)?.OnRoundEnded();
    }

    private void OnPlayerSpawned()
    {
        (CurrentState as AthState)?.OnPlayerSpawned();
    }

    private void OnCrossedFinishLine(float time)
    {
        (CurrentState as AthState)?.OnCrossedFinishLine(time);
    }

    private void OnLevelLoaded()
    {
        (CurrentState as AthState)?.OnLevelLoaded();
    }

    private void OnPhotoModeEntered()
    {
        (CurrentState as AthState)?.OnPhotoModeEntered();
    }

    private void SubscribeEvents()
    {
        if (_eventsSubscribed)
        {
            return;
        }

        Timer.Tick += OnAthTimerTick;
        RacingApi.RoundStarted += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
        RacingApi.PlayerSpawned += OnPlayerSpawned;
        RacingApi.CrossedFinishLine += OnCrossedFinishLine;
        RacingApi.LevelLoaded += OnLevelLoaded;
        PhotoModeApi.PhotoModeEntered += OnPhotoModeEntered;

        _eventsSubscribed = true;
    }

    private void UnsubscribeEvents()
    {
        if (!_eventsSubscribed)
        {
            return;
        }

        Timer.Tick -= OnAthTimerTick;
        RacingApi.RoundStarted -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
        RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
        RacingApi.LevelLoaded -= OnLevelLoaded;
        PhotoModeApi.PhotoModeEntered -= OnPhotoModeEntered;

        _eventsSubscribed = false;
    }

    public void StartTimer()
    {
        if (_timerStarted)
        {
            return;
        }

        Timer.Start();
        SubscribeEvents();
        _timerStarted = true;
    }

    public void StopTimer()
    {
        if (!_timerStarted)
        {
            return;
        }

        Timer.Stop();

        UnsubscribeEvents();
        _timerStarted = false;
    }
}