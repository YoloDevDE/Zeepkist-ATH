using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		AthStateMachine.Services.LevelSummary.Show(AthStateMachine.Ctx);

		StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
	}
}
