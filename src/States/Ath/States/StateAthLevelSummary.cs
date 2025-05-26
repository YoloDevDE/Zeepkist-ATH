using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary : IState
{
    public StateAthLevelSummary(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    private AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        RacingApi.PlayerSpawned += OnPlayerSpawned;
    }

    public void Execute()
    {
        if (AthStateMachine.Ctx.CurrentLevel == null)
        {
            return;
        }

        UpdateUI();
        UpdateLevelInfo();
    }

    public void Exit()
    {
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
    }

    private void OnPlayerSpawned()
    {
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    private void UpdateLevelInfo()
    {
        AthStateMachine.Ctx.CurrentLevel.EndTime = DateTime.Now;
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageLevelSummary());
    }

    private void UpdateUI()
    {
        string currentLevelStatus = AthStateMachine.Ctx.CurrentLevel.Status;
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = $"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>{currentLevelStatus}</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
    }
}