using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForReplacementLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	// Properties

	public override async void Enter()
	{
		OnlineZeeplevel brokenLevel;

		// The playlist entry at the current index is the one that failed to load - the
		// game silently fell back to another level, which is exactly how
		// StateAthWaitingForLevelData.IsBrokenLevel() detected the breakage. Ctx.CurrentLevel
		// is NOT this level: only StateAthWaitingForFirstRun writes into the context, so
		// on this path it still holds the previous level.
		try
		{
			int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
			brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
		}
		catch (Exception ex)
		{
			Logger.LogError($"StateAthWaitingForReplacementLevel: Could not read the broken playlist entry: {ex.Message}");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
			return;
		}

		try
		{
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"StateAthWaitingForReplacementLevel: Could not send broken level message: {ex.Message}");
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.ReplaceLevelInCurrentPlaylist(brokenLevel, newLevel);

			// Restarting into a level Steam has not finished writing is what made the last one
			// look broken. Doing it again here is how one bad moment turned into a loop that
			// ate the level pool a level at a time.
			await AthStateMachine.Services.WorkshopDownloads.WaitUntilReadyAsync(newLevel);

			// The wait is seconds long, and the run can be stopped inside it.
			if (StateMachine.CurrentState != this)
			{
				return;
			}

			PlaylistService.RestartCurrentLevel();
		}
		catch (Exception ex)
		{
			// async void - nothing above us can catch this.
			Logger.LogError($"StateAthWaitingForReplacementLevel: Could not draw a replacement level: {ex.Message}");
			ToastNotification.Error("Could not find a replacement for the broken level");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}


	public override void OnLevelLoaded()
	{
		StateMachine.TransitionTo(new StateAthWaitingForLevelData(AthStateMachine));
	}
}
