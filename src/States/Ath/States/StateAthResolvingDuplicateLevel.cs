using System;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingDuplicateLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private const int MaxConsecutiveDuplicates = 3;


	public override async void Execute()
	{
		AthStateMachine.Ctx.ConsecutiveDuplicateCount++;

		if (AthStateMachine.Ctx.ConsecutiveDuplicateCount >= MaxConsecutiveDuplicates)
		{
			int retries = AthStateMachine.Ctx.ConsecutiveDuplicateCount;
			Logger.LogWarning(
				$"StateAthResolvingDuplicateLevel: Duplicate limit reached after {retries} retries. Ending run.");
			AthStateMachine.Show(AthStateMachine.Ctx.Messages.DuplicateLimitReached(retries));
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
			// async void - nothing above us can catch this.
			Logger.LogError($"StateAthResolvingDuplicateLevel: Could not draw a replacement level: {ex.Message}");
			Overlay.Notify("Could not find another level to play", HudPalette.Danger);
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	public override void OnLevelLoaded()
	{
		StateMachine.TransitionTo(new StateAthProcessingLevel(AthStateMachine));
	}
}