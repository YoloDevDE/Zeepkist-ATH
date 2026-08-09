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

public class StateAthWaitingForLevelData(AthController controller) : AthState(controller)
{
	private const int _maxConsecutiveBrokenLevels = 5;

	public override async void Enter()
	{
		AthController.Services.LevelSummary.Hide();

		try
		{
			if (IsNull(LevelApi.CurrentLevel))
			{
				await Task.Delay(100);
				Controller.TransitionTo(new StateAthWaitingForLevelData(AthController));
				return;
			}

			Level currentLevel = new(LevelApi.CurrentLevel);

			if (IsBrokenLevel(currentLevel))
			{
				AthController.Ctx.ConsecutiveBrokenCount++;

				if (AthController.Ctx.ConsecutiveBrokenCount >= _maxConsecutiveBrokenLevels)
				{
					Logger.LogError(
						$"StateAthWaitingForLevelData: {_maxConsecutiveBrokenLevels} levels in a row failed to load, stopping the run.");
					FrogNotification.Error("Levels keep failing to load - the hunt was stopped");
					Controller.TransitionTo(new StateAthStopping(AthController));
					return;
				}

				Controller.TransitionTo(new StateAthWaitingForReplacementLevel(AthController));
				return;
			}

			if (IsDuplicateLevel(currentLevel))
			{
				Controller.TransitionTo(new StateAthWaitingForExtraLevel(AthController));
				return;
			}

			AthController.Ctx.ConsecutiveDuplicateCount = 0;
			AthController.Ctx.ConsecutiveBrokenCount = 0;
			Controller.TransitionTo(new StateAthWaitingForFirstRun(AthController));
		}
		catch (Exception ex)
		{
			Logger.LogError(
				$"StateAthWaitingForLevelData: Unhandled exception, ending the run: {ex.Message}\nStack trace: {ex.StackTrace}");
			FrogNotification.Error("Could not process the level - the hunt was stopped");
			Controller.TransitionTo(new StateAthStopping(AthController));
		}
	}

	/// <summary>
	///     Whether the level that loaded is not the one the playlist asked for, which is what a
	///     server skipping an entry it cannot serve looks like from in here.
	///     The UID is the honest answer and the name is the fallback, because the two are not
	///     always the same level's idea of itself: the backend hands out a fileUid that some
	///     levels do not carry in their own data - 'Level D-01' comes back as <c>ead1</c> and
	///     loads as <c>55MzxAULxUegEfl_PlayerName</c>. Judged on the UID alone that level is
	///     broken every single time it loads, so the run replaced it, restarted, drew the same
	///     verdict again and never got out of the loading screen.
	/// </summary>
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

		OnlineZeeplevel expected = playlist[currentIndex];

		if (expected.UID == level.LevelUid)
		{
			return false;
		}

		if (expected.Name != level.Name)
		{
			return true;
		}

		Logger.LogWarning(
			$"StateAthWaitingForLevelData: '{level.Name}' loaded with UID {level.LevelUid} where the playlist says {expected.UID}, accepting it on its name.");

		return false;
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
		return AthController.Ctx.Settings.RejectDuplicateLevels && AthController.Ctx.Levels.Contains(level);
	}
}
