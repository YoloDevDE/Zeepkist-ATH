using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary : AthState
{
    public StateAthLevelSummary(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    private AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;
    public override IStateMachine StateMachine { get; }

    public override void Enter()
    {
    }

    public override void Execute()
    {
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageLevelSummary());
        string currentLevelStatus = AthStateMachine.Ctx.CurrentLevel.Status;
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = $"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>{currentLevelStatus}</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public override void Exit()
    {
    }

    public override void OnAthTimerTick()
    {
    }
}