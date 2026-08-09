using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(AthController controller) : AthState(controller)
{
	public override void Enter()
	{
		AthController.Services.LevelSummary.Show(AthController.Ctx);

		Controller.TransitionTo(new StateAthLoadingLevel(AthController));
	}
}
