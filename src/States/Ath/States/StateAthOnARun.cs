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

public class StateAthOnARun(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.RoundStarted += OnRoundStarted;
        RacingApi.RoundEnded += OnRoundEnded;

        RacingApi.CrossedFinishLine += OnCrossedFinishLine;
        AthStateMachine.Ctx.CurrentLevel.AddTimeStamp();
    }

    public override void Execute()
    {
        OnAthTimerTick();
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageOnARun());
    }

    public override void Exit()
    {
        AthStateMachine.Ctx.CurrentLevel.AddTimeStamp();
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

        if (currentLevel.Status == Level.LevelStatus.AUTHOR)
        {
            StateMachine.TransitionTo(new StateAthWaitingForRespawn(StateMachine));
            return;
        }

        if (currentResult.Time <= currentLevel.GoldTime && !currentLevel.GoldMedalAcquired)
        {
            Messenger.Notify().LogCustomColors("Gold Medal acquired!<br>You can now skip without penalty", Color.black, new Color(1f, 0.84f, 0f), 10f);
        }

        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageCrossedFinishLine());
    }

    private void OnRoundStarted()
    {
        AthStateMachine.Ctx.CurrentLevel.Attempt++;
        Execute();
    }

    public override void OnAthTimerTick()
    {
        if (AthStateMachine.Ctx.IsTimeOver())
        {
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        AthStateMachine.SetServerMessage(false);

        if (AthStateMachine.Ctx.CheckAndNotifyTimeRunningLow())
        {
            Messenger.Notify().LogCustomColors("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", Color.white, Color.red, 10f);
        }
    }
}