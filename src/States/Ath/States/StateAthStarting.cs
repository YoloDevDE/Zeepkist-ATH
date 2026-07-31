using System;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using TMPro;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStarting(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private CancellationTokenSource _cts;
	// Constructor

	// Properties

	public static bool IsCountdownActive { get; private set; }

	// Public Methods

	public override async void Execute()
	{
		try
		{
			// Sende Startmeldung
			ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.Messages.Starting());

			// ATH owns the clock - the lobby round timer must not cut a level short.
			// This used to sit in the non-RTM branch only, so in the default (RTM)
			// mode the lobby timer stayed live.
			ZeepkistNetwork.CurrentLobby.RoundTime = 86400;

			if (!Plugin.Instance.MyConfig.RandomPlaylist.Value)
			{
				await Task.Delay(2500);
				MultiplayerApi.UpdateServerPlaylist();
				await Task.Delay(500);
				await RunCountdown();
				PlaylistService.SkipLevel();
				StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
				return;
			}

			// Starte neue Playlist mit Fehlerbehandlung
			bool playlistStarted = false;
			int retryCount = 3; // Maximal 3 Versuche

			while (!playlistStarted && retryCount > 0)
				try
				{
					OnlineZeeplevel level = await RandomLevelService.Instance.DrawRandomLevelAsync();
					PlaylistService.StartNewPlaylist(level);
					playlistStarted = true;
				}
				catch (Exception ex)
				{
					retryCount--;
					Logger.LogError($"Failed to start playlist: {ex.Message}");

					if (retryCount <= 0)
					{
						Messenger.Notify().LogError("Failed to start playlist after multiple attempts");
						// Weiter zum nächsten Schritt trotz Fehler
					}

					// Kurze Pause vor dem nächsten Versuch
					await Task.Delay(500);
				}

			await RunCountdown();

			// Versuche zum nächsten Level zu springen, auch wenn die Playlist nicht gestartet wurde
			try
			{
				PlaylistService.SkipToFirstLevel();
			}
			catch (Exception ex)
			{
				Logger.LogError($"Failed to skip level: {ex.Message}");
				Messenger.Notify().LogError("Error while skipping to the first level");
			}

			StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
		}
		catch (Exception ex)
		{
			Logger.LogError($"Execute failed: {ex.Message}\nStack trace: {ex.StackTrace}");
			Messenger.Notify().LogError("Something went wrong while starting the hunt");

			// Optional: Transition zu einem Fehler-State oder Reset-State
			// StateMachine.TransitionTo(new StateAthError(AthStateMachine));
		}
	}

	public override void Exit()
	{
		_cts?.Cancel();
	}

	private async Task RunCountdown()
	{
		_cts = new CancellationTokenSource();
		IsCountdownActive = true;
		TMP_Text text = PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText;

		try
		{
			for (int i = 5; i >= 1; i--)
			{
				text.SetText(
					$"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>Starting in <b>{i}</b>...");
				// Used to be .ContinueWith(_ => { }), which swallowed not just the
				// cancellation but every other exception along with it.
				await Task.Delay(1000, _cts.Token);
			}
		}
		catch (OperationCanceledException)
		{
			// Exit() cancelled us because the state is going away. Nothing to clean up
			// beyond the finally block.
		}
		finally
		{
			IsCountdownActive = false;
			// Null first: Exit() may still call Cancel(), and that throws on a disposed source.
			CancellationTokenSource cts = _cts;
			_cts = null;
			cts?.Dispose();
		}
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthLoadingLevel(AthStateMachine));
	}
}