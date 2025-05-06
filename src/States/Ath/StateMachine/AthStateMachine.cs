using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public class AthStateMachine : IStateMachine
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
        string stateColor = paused ? "#ffff00" : "#0088ff";
        string stateText = paused ? "paused" : "running";
        string timeLeftColor = paused || Ctx.CurrentDuration.TotalSeconds > Ctx.PunishTime ? stateColor : "#ff0000";
        string currentLevelColor = paused ? "#ffff00" : "#0088ff";

        string message = $"/servermessage white 0 <size=\"25%\"><align=\"left\"><b>Author-Time-Hunting</b><br>" +
                         $"<#ffffff>State         : <{stateColor}>{stateText}<br>" +
                         $"<#ffffff>Time Left     : <{timeLeftColor}>{TimeFormatter.FormatDuration((int)Ctx.CurrentDuration.TotalSeconds)}<br>" +
                         $"<#ffffff>Current Level : <{currentLevelColor}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.Duration.TotalSeconds)}<br>" +
                         $"<#ffffff>Current Skip  : {(Ctx.CurrentLevel.Levelbeaten ? "<#AF00AF>Author Skip" : Ctx.CurrentLevel.GoldSkipUnlocked ? "<#FFD600>Gold Skip" : Ctx.FreeSkips > 0 ? $"<#00ffff>Free Skip ({Ctx.FreeSkips}x left)" : Ctx.TimeIsRunningLow ? "<#880000>!END RUN SKIP!" : "<#FF0000>Penalty Skip!")}<br>" +
                         $"<#ffffff>Attempt       : {Ctx.CurrentLevel.Attempt}<br>" +
                         $"<#ffffff>-----------Results-----------<br>" +
                         $"<#ffffff>AT/Gold/None  : <#AF00AF>{Ctx.AuthorMedals}<#ffffff>/<#FFD600>{Ctx.GoldMedals}<#ffffff>/<#FF0000>{Ctx.Skips - Ctx.GoldMedals}<br>"
            ;

        ChatApi.SendMessage(message);
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