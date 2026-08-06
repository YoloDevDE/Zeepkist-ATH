using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Level;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForLevelData(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private const int MaxConsecutiveBrokenLevels = 5;

	public override async void Enter()
	{
		AthStateMachine.Services.LevelSummary.Hide();

		try
		{
			if (IsNull(LevelApi.CurrentLevel))
			{
				await Task.Delay(100);
				StateMachine.TransitionTo(new StateAthWaitingForLevelData(AthStateMachine));
				return;
			}

			Level currentLevel = new(LevelApi.CurrentLevel);

			if (IsBrokenLevel(currentLevel))
			{
				AthStateMachine.Ctx.ConsecutiveBrokenCount++;

				if (AthStateMachine.Ctx.ConsecutiveBrokenCount >= MaxConsecutiveBrokenLevels)
				{
					Logger.LogError(
						$"StateAthWaitingForLevelData: {MaxConsecutiveBrokenLevels} levels in a row failed to load, stopping the run.");
					FrogNotification.Error("Levels keep failing to load - the hunt was stopped");
					StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
					return;
				}

				StateMachine.TransitionTo(new StateAthWaitingForReplacementLevel(AthStateMachine));
				return;
			}

			if (IsDuplicateLevel(currentLevel))
			{
				StateMachine.TransitionTo(new StateAthWaitingForExtraLevel(AthStateMachine));
				return;
			}

			AthStateMachine.Ctx.ConsecutiveDuplicateCount = 0;
			AthStateMachine.Ctx.ConsecutiveBrokenCount = 0;
			StateMachine.TransitionTo(new StateAthWaitingForFirstRun(AthStateMachine));
		}
		catch (Exception ex)
		{
			Logger.LogError(
				$"StateAthWaitingForLevelData: Unhandled exception, ending the run: {ex.Message}\nStack trace: {ex.StackTrace}");
			FrogNotification.Error("Could not process the level - the hunt was stopped");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	private bool IsBrokenLevel(Level level)
	{
		List<OnlineZeeplevel> playlist = ZeepkistNetwork.CurrentLobby.Playlist;
		int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;

		if (currentIndex < 0 || currentIndex >= playlist.Count)
		{
			Logger.LogWarning(
				$"StateAthWaitingForLevelData: Playlist index {currentIndex} is outside a playlist of {playlist.Count} entries, accepting '{level.Name}' as loaded.");
			return false;
		}

		return !playlist[currentIndex].UID.Equals(level.LevelUid);
	}

	private bool IsNull(LevelScriptableObject level)
	{
		if (level != null)
		{
			return false;
		}

		Logger.LogError("OnPlayerSpawned: Failed to create Level object");
		return true;
	}

	private bool IsDuplicateLevel(Level level)
	{
		return AthStateMachine.Ctx.Settings.RejectDuplicateLevels && AthStateMachine.Ctx.Levels.Contains(level);
	}
}
