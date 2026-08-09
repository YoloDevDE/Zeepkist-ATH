using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForExtraLevel(AthController controller) : AthState(controller)
{
	private const int _maxConsecutiveDuplicates = 3;

	public override async void Enter()
	{
		AthController.Ctx.ConsecutiveDuplicateCount++;

		if (AthController.Ctx.ConsecutiveDuplicateCount >= _maxConsecutiveDuplicates)
		{
			int retries = AthController.Ctx.ConsecutiveDuplicateCount;
			Logger.LogWarning(
				$"StateAthWaitingForExtraLevel: Duplicate limit reached after {retries} retries. Ending run.");
			Controller.TransitionTo(new StateAthStopping(AthController));
			return;
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.AddLevelToCurrentPlaylist(newLevel);
			PlaylistService.SkipToNextLevel();
		}
		catch (Exception ex)
		{
			Logger.LogError($"StateAthWaitingForExtraLevel: Could not draw a replacement level: {ex.Message}");
			FrogNotification.Error("Could not find another level to play");
			Controller.TransitionTo(new StateAthStopping(AthController));
		}
	}

	public override void OnLevelLoaded()
	{
		Controller.TransitionTo(new StateAthWaitingForLevelData(AthController));
	}
}
