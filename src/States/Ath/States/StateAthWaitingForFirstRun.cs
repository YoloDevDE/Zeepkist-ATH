using System;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using ZeepSDK.Level;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForFirstRun(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		try
		{
			AthStateMachine.Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
			AthStateMachine.SetServerMessage(true);
		}
		catch (Exception e)
		{
			Logger.LogError(
				$"StateAthWaitingForFirstRun: Failed to start level: {e.Message}\nStack trace: {e.StackTrace}");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	private async Task AddLevelAsync()
	{
		if (!AthStateMachine.Ctx.Settings.RandomPlaylist)
		{
			return;
		}

		try
		{
			Logger.LogInfo("Adding Random Level");
			OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.AddLevelToCurrentPlaylist(level);
		}
		catch (Exception e)
		{
			Logger.LogError($"StateAthWaitingForFirstRun: Could not pre-load the next level: {e.Message}");
			FrogNotification.Warn("Could not load the next level - the playlist may run out");
		}
	}

	public override void OnRoundStarted()
	{
		_ = AddLevelAsync();
		StateMachine.TransitionTo(new StateAthWaitingForFinish(AthStateMachine));
	}
}
