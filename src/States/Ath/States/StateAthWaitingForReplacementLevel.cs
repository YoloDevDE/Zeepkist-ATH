using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForReplacementLevel(AthStateMachine stateMachine) : AthState(stateMachine)
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
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
			return;
		}

		try
		{
		}
		catch (Exception ex)
		{
			Logger.LogWarning(
				$"StateAthWaitingForReplacementLevel: Could not send broken level message: {ex.Message}");
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.ReplaceLevelInCurrentPlaylist(brokenLevel, newLevel);

			await AthStateMachine.Services.WorkshopDownloads.WaitUntilReadyAsync(newLevel);

			if (StateMachine.CurrentState != this)
			{
				return;
			}

			PlaylistService.RestartCurrentLevel();
		}
		catch (Exception ex)
		{
			Logger.LogError($"StateAthWaitingForReplacementLevel: Could not draw a replacement level: {ex.Message}");
			FrogNotification.Error("Could not find a replacement for the broken level");
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
		}
	}

	public override void OnLevelLoaded()
	{
		StateMachine.TransitionTo(new StateAthWaitingForLevelData(AthStateMachine));
	}
}
