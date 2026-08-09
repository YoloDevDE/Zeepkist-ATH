using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     Nothing here waits on a clock any more. A run used to sit through five silent seconds
///     before skipping to its first level, which was a guess at how long the playlist takes to
///     reach the server. The playlist service says when the push actually went out, so the
///     skip goes out right behind it - and the player, who has just watched a ten second
///     countdown on the welcome screen, gets the level they were counting down to.
/// </summary>
public class StateAthStarting(AthController controller) : AthState(controller)
{
	private CancellationTokenSource _cts;

	public override async void Enter()
	{
		_cts = new CancellationTokenSource();
		CancellationToken stopped = _cts.Token;

		try
		{
			ZeepkistNetwork.CurrentLobby.RoundTime = 86400;

			if (!AthController.Ctx.Settings.RandomPlaylist)
			{
				await Task.Delay(2500, stopped);
				MultiplayerApi.UpdateServerPlaylist();
				await Task.Delay(500, stopped);
				Controller.TransitionTo(new StateAthLoadingLevel(AthController));
				return;
			}

			OnlineZeeplevel firstLevel = null;
			int retryCount = 3;

			while (firstLevel == null && retryCount > 0)
			{
				try
				{
					OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();
					await PlaylistService.StartNewPlaylist(level);
					firstLevel = level;
				}
				catch (Exception ex)
				{
					retryCount--;
					Logger.LogError($"Failed to start playlist: {ex.Message}");

					if (retryCount <= 0)
					{
						FrogNotification.Error("Failed to start playlist after multiple attempts");
					}

					await Task.Delay(500, stopped);
				}
			}

			// The setup screen is still up and its watchdog only counts silence. Downloading a
			// level off the workshop is the longest wait in the whole setup and the only one that
			// arrives without a step of its own, so it says so before it starts.
			AthController.Services.Loading.Step("Downloading the level");

			await AthController.Services.WorkshopDownloads.WaitUntilReadyAsync(firstLevel);

			try
			{
				PlaylistService.SkipToFirstLevel();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Failed to skip level: {ex.Message}");
				FrogNotification.Error("Error while skipping to the first level");
			}

			Controller.TransitionTo(new StateAthLoadingLevel(AthController));
		}
		catch (OperationCanceledException)
		{
		}
		catch (Exception ex)
		{
			Logger.LogError($"Enter failed: {ex.Message}\nStack trace: {ex.StackTrace}");
			FrogNotification.Error("Something went wrong while starting the hunt");
		}
	}

	public override void Exit()
	{
		_cts?.Cancel();
		_cts?.Dispose();
		_cts = null;
	}

	public override void OnRoundEnded()
	{
		Controller.TransitionTo(new StateAthLoadingLevel(AthController));
	}
}
