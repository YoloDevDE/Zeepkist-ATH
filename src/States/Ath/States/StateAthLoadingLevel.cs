using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingLevel(AthController controller) : AthState(controller)
{
	public override void OnLevelLoaded()
	{
		if (!AthController.Ctx.IsTimeOver() && (AthController.Ctx.Settings.RandomPlaylist ||
		                                          AthController.Ctx.Levels.Count <
		                                          ZeepkistNetwork.CurrentLobby.Playlist.Count))
		{
			Controller.TransitionTo(new StateAthWaitingForLevelData(AthController));
			return;
		}

		Controller.TransitionTo(new StateAthStopping(AthController));
	}
}
