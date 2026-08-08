using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void OnLevelLoaded()
	{
		if (!AthStateMachine.Ctx.IsTimeOver() && (AthStateMachine.Ctx.Settings.RandomPlaylist ||
		                                          AthStateMachine.Ctx.Levels.Count <
		                                          ZeepkistNetwork.CurrentLobby.Playlist.Count))
		{
			StateMachine.TransitionTo(new StateAthWaitingForLevelData(AthStateMachine));
			return;
		}

		StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
	}
}
