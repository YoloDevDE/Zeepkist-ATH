using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForReplacementLevel(AthController controller) : AthState(controller)
{
	public override async void Enter()
	{
		OnlineZeeplevel brokenLevel;

		try
		{
			int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
			brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
		}
		catch (Exception ex)
		{
			Logger.LogError(
				$"StateAthWaitingForReplacementLevel: Could not read the broken playlist entry: {ex.Message}");
			Controller.TransitionTo(new StateAthStopping(AthController));
			return;
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.ReplaceLevelInCurrentPlaylist(brokenLevel, newLevel);

			await AthController.Services.WorkshopDownloads.WaitUntilReadyAsync(newLevel);

			if (Controller.CurrentState != this)
			{
				return;
			}

			PlaylistService.RestartCurrentLevel();
		}
		catch (Exception ex)
		{
			Logger.LogError($"StateAthWaitingForReplacementLevel: Could not draw a replacement level: {ex.Message}");
			FrogNotification.Error("Could not find a replacement for the broken level");
			Controller.TransitionTo(new StateAthStopping(AthController));
		}
	}

	public override void OnLevelLoaded()
	{
		Controller.TransitionTo(new StateAthWaitingForLevelData(AthController));
	}
}
