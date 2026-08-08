using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForExtraLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private const int _maxConsecutiveDuplicates = 3;

	public override async void Enter()
	{
		AthStateMachine.Ctx.ConsecutiveDuplicateCount++;

		if (AthStateMachine.Ctx.ConsecutiveDuplicateCount >= _maxConsecutiveDuplicates)
		{
			int retries = AthStateMachine.Ctx.ConsecutiveDuplicateCount;
			Logger.LogWarning(
				$"StateAthWaitingForExtraLevel: Duplicate limit reached after {retries} retries. Ending run.");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
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
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	public override void OnLevelLoaded()
	{
		StateMachine.TransitionTo(new StateAthWaitingForLevelData(AthStateMachine));
	}
}
