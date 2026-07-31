using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Execute()
	{
		AthStateMachine.Show(AthStateMachine.Ctx.Messages.LevelSummary());
		StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
	}
}