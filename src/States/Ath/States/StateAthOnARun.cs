using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthOnARun : AthState
{
    public StateAthOnARun(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;
    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;
        RacingApi.CrossedFinishLine += OnCrossedFinishLine;
    }

    public override void Execute()
    {
        AthStateMachine.SetServerMessage(false);
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageOnARun());
    }

    public override void Exit()
    {
        RacingApi.RoundStarted -= OnRoundStarted;
        RacingApi.RoundEnded -= OnRoundEnded;
        RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthEvaluateSkip(StateMachine));
    }

    private void OnCrossedFinishLine(float time)
    {
        ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
        PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
        Level currentLevel = AthStateMachine.Ctx.CurrentLevel;

        if (currentResult == null)
        {
            StateMachine.TransitionTo(new StateAthPausing(StateMachine));
            return;
        }

        currentLevel.PersonalBestTime = currentResult.Time;

        if (currentResult.Time <= currentLevel.AuthorTime)
        {
            Messenger.Notify().LogCustomColors("Author Medal acquired!<br>[Respawn to continue]", Color.white, new Color(0.5f, 0f, 0.5f), 10f);
            StateMachine.TransitionTo(new StateAthWaitingForRespawn(StateMachine));
            return;
        }

        if (currentResult.Time <= currentLevel.GoldTime && !currentLevel.GoldSkipUnlocked)
        {
            currentLevel.GoldSkipUnlocked = true;
            Messenger.Notify().LogCustomColors("Gold Medal acquired!<br>You can now skip without penalty", Color.black, new Color(1f, 0.84f, 0f), 10f);
        }

        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageLevelResult());
    }

    private void OnRoundStarted()
    {
        AthStateMachine.Ctx.CurrentLevel.Attempt++;
        Execute();
    }

    public override void OnAthTimerTick()
    {
        AthStateMachine.Ctx.CurrentLevel.EndTime = DateTime.Now;
        AthStateMachine.SetServerMessage(false);
        if (!AthStateMachine.Ctx.TimeIsRunningLow && AthStateMachine.Ctx.CurrentDuration.TotalSeconds <= AthStateMachine.Ctx.PunishTime)
        {
            AthStateMachine.Ctx.TimeIsRunningLow = true;
            Messenger.Notify().LogCustomColors("Time is running low!<br>A 'Penalty-Skip' will end the run!", Color.white, Color.red, 10f);
        }
    }
}