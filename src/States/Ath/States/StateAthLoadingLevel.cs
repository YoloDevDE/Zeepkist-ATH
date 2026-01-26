using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingLevel(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;


    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }


    public override void Execute()
    {
        // PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text =
        //     $"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Level: <b>{AthStateMachine.Ctx.CurrentLevel.StatusString}</b>";
        // PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    public override void OnAthTimerTick() { }

    private void OnLevelLoaded()
    {
        if (AthStateMachine.Ctx.IsTimeOver())
        {
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }
}