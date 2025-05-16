using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthOnARun : IState
{
    // Private Fields

    // Constructor
    public StateAthOnARun(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        AthStateMachine.Timer.Tick += TimerOnTick;
        RacingApi.RoundStarted += OnRoundNztStarted;
        RacingApi.CrossedFinishLine += OnCrossedFinishLine;
        RacingApi.RoundEnded += OnRoundNztEnded;
    }


    public void Execute()
    {
        if (AthStateMachine.Ctx.CurrentLevel.FirstTimePlayed)
        {
            AthStateMachine.Ctx.CurrentLevel.FirstTimePlayed = false;
            AthStateMachine.Ctx.CurrentLevel.StartTime = DateTime.Now;
        }

        AthStateMachine.SetServerMessage(false);
        AthStateMachine.Ctx.CurrentLevel.Attempt++;
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageRunning());
    }

    public void Exit()
    {
        AthStateMachine.Timer.Tick -= TimerOnTick;
        RacingApi.RoundStarted -= OnRoundNztStarted;
        RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
        RacingApi.RoundEnded -= OnRoundNztEnded;
    }

    private void OnRoundNztEnded()
    {
        StateMachine.TransitionTo(new StateAthEvaluateSkip(StateMachine));
    }

    private void OnCrossedFinishLine(float time)
    {
        StateMachine.TransitionTo(new StateAthEvaluateRun(StateMachine));
    }

    private void OnRoundNztStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }

    private void TimerOnTick()
    {
        AthStateMachine.SetServerMessage(false);
        if (!AthStateMachine.Ctx.TimeIsRunningLow && AthStateMachine.Ctx.CurrentDuration.TotalSeconds <= AthStateMachine.Ctx.PunishTime)
        {
            AthStateMachine.Ctx.TimeIsRunningLow = true;
            Messenger.Notify().LogCustomColors("Time is running low!<br>A 'Penalty-Skip' will end the run!", Color.white, Color.red, 10f);
        }
    }
}