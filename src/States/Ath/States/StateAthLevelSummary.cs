using AuthorTimeHunting.States.Ath.StateMachine;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLevelSummary(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		// Snapshotted here and nowhere else: this is the last state that still has the level
		// that just ended as CurrentLevel. The card itself stays up through the loading screen
		// and is taken down when the player is driving again.
		AthStateMachine.Services.LevelSummary.Show(AthStateMachine.Ctx);

		StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
	}
}
