using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingBrokenLevel(AthStateMachine stateMachine) : AthState(stateMachine)
{
	// Properties

	public override async void Execute()
	{
		OnlineZeeplevel brokenLevel;

		// The playlist entry at the current index is the one that failed to load - the
		// game silently fell back to another level, which is exactly how
		// StateAthProcessingLevel.IsBrokenLevel() detected the breakage. Ctx.CurrentLevel
		// is NOT this level: only StateAthStartLevelFirstTime writes into the context, so
		// on this path it still holds the previous level.
		try
		{
			int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
			brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
		}
		catch (Exception ex)
		{
			Logger.LogError($"StateAthResolvingBrokenLevel: Could not read the broken playlist entry: {ex.Message}");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
			return;
		}

		try
		{
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"StateAthResolvingBrokenLevel: Could not send broken level message: {ex.Message}");
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.ReplaceLevelInCurrentPlaylist(brokenLevel, newLevel);
			PlaylistService.RestartCurrentLevel();
		}
		catch (Exception ex)
		{
			// async void - nothing above us can catch this.
			Logger.LogError($"StateAthResolvingBrokenLevel: Could not draw a replacement level: {ex.Message}");
			Messenger.Notify().LogError("Could not find a replacement for the broken level");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}


	public override void OnLevelLoaded()
	{
		StateMachine.TransitionTo(new StateAthProcessingLevel(AthStateMachine));
	}
}