using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : MonoBehaviour, IStateMachine
{
    public AthStateMachine()
    {
        Ctx = new AthCtx();
        Timer = new AthTimer();
        InitialState = new StateAthStarting(this);
        FinalState = new StateAthStopping(this);
    }

    public AthTimer Timer { get; set; }
    public AthCtx Ctx { get; set; }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.F2))
        {
            Messenger.Notify().Log("Test");
        }
    }


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
        // string stateColor = paused ? "#ffff00" : "#0088ff";
        // string stateText = paused ? "Paused" : "Running";
        // string timeLeftColor = paused || Ctx.CurrentDuration.TotalSeconds > Ctx.PunishTime ? stateColor : "#ff0000";
        // string currentLevelColor = paused ? "#ffff00" : "#0088ff";
        // string currentSkip = Ctx.CurrentLevel.Levelbeaten
        //     ? "<#AF00AF>Author Skip"
        //     : Ctx.CurrentLevel.GoldSkipUnlocked
        //         ? "<#FFD600>Gold Skip"
        //         : Ctx.FreeSkips > 0
        //             ? $"<#00ffff>Free Skip ({Ctx.FreeSkips}x left)"
        //             : Ctx.TimeIsRunningLow
        //                 ? "<#880000>!END RUN SKIP!"
        //                 : "<#FF0000>Penalty Skip!";
        //
        // UIBuilder uiBuilder = UIBuilder.Create(
        //     new Vector2(50, 50),
        //     new Vector2(500, 400),
        //     "Author-Time-Hunting",
        //     new Color(0, 0, 0, 0.8f) // Semi-transparent black
        // );
        //
        // uiBuilder.AddTMPLabel("<b><color=#FFA500>Author-Time-Hunting</color></b>")
        //     .AddSpace(10)
        //     .AddTMPLabel($"<color=#ffffff>State         :</color> <color={stateColor}>{stateText}</color>")
        //     .AddTMPLabel($"<color=#ffffff>Time Left     :</color> <color={timeLeftColor}>{TimeFormatter.FormatDuration((int)Ctx.CurrentDuration.TotalSeconds)}</color>")
        //     .AddTMPLabel($"<color=#ffffff>Current Level :</color> <color={currentLevelColor}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.Duration.TotalSeconds)}</color>")
        //     .AddTMPLabel($"<color=#ffffff>Current Skip  :</color> {currentSkip}")
        //     .AddTMPLabel($"<color=#ffffff>Attempt       :</color> {Ctx.CurrentLevel.Attempt}")
        //     .AddHorizontalLine()
        //     .AddTMPLabel("<color=#ffffff>----------- Results -----------</color>")
        //     .AddTMPLabel($"<color=#ffffff>AT/Gold/None  :</color> <color=#AF00AF>{Ctx.AuthorMedals}</color>/<color=#FFD600>{Ctx.GoldMedals}</color>/<color=#FF0000>{Ctx.Skips - Ctx.GoldMedals}</color>")
        //     .AddButton("Close", () => Plugin.Instance.MainGUI.ToggleVisibility());
        //
        // Plugin.Instance.MainGUI.SetDynamicUI(uiBuilder);
    }


    public void StartTimer()
    {
        Timer.Start();
    }

    public void StopTimer()
    {
        Timer.Stop();
    }
}