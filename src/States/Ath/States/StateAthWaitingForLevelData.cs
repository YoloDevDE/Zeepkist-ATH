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
	/// <summary>
	///     How many levels in a row may fail to load before the run gives up. Three is a bad
	///     evening on the workshop; five is not the levels.
	/// </summary>
	private const int MaxConsecutiveBrokenLevels = 5;


	public override async void Enter()
	{
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
					// Levels do not come in runs of five broken ones. Something else is wrong -
					// Steam mid-write, a lobby that will not take the playlist - and replacing
					// level after level only spends the pool while nothing improves.
					Logger.LogError(
						$"StateAthWaitingForLevelData: {MaxConsecutiveBrokenLevels} levels in a row failed to load, stopping the run.");
					ToastNotification.Error("Levels keep failing to load - the hunt was stopped");
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

			// A level we actually keep ends both streaks.
			AthStateMachine.Ctx.ConsecutiveDuplicateCount = 0;
			AthStateMachine.Ctx.ConsecutiveBrokenCount = 0;
			StateMachine.TransitionTo(new StateAthWaitingForFirstRun(AthStateMachine));
		}
		catch (Exception ex)
		{
			// Ending the run is a heavy answer to a failure here, so it must not be a silent
			// one: without the toast this looked like the run stopping itself for no reason.
			Logger.LogError(
				$"StateAthWaitingForLevelData: Unhandled exception, ending the run: {ex.Message}\nStack trace: {ex.StackTrace}");
			ToastNotification.Error("Could not process the level - the hunt was stopped");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	/// <summary>
	///     True when the level the game actually loaded is not the one the playlist says is
	///     current. That is what a level failing to load looks like from here: the lobby moves
	///     on, the scene does not.
	/// </summary>
	private bool IsBrokenLevel(Level level)
	{
		List<OnlineZeeplevel> playlist = ZeepkistNetwork.CurrentLobby.Playlist;
		int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;

		if (currentIndex < 0 || currentIndex >= playlist.Count)
		{
			// Being out of step with the lobby is not the same as the level being broken, and
			// this used to throw rather than say so - straight into the catch below, which
			// ends the run. There is nothing to compare against, so trust what was loaded.
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
