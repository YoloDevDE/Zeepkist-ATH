using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Execute()
	{
		ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.Messages.LevelSummary());
		StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
	}
}