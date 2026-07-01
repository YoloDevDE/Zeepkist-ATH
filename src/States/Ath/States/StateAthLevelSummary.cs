using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }

    public override void Execute()
    {
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageLevelSummary());
        StateMachine.TransitionTo(new StateAthLoadingLevel(StateMachine));
    }

    public override void Exit() { }

    public override void OnAthTimerTick() { }
}