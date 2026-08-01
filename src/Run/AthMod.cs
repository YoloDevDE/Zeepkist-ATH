using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.Run;

/// <summary>
///     The mod's own lifecycle: idle until /ath start, running until /ath stop, without a
///     state machine.
///     Where the state machine variant has two states that subscribe to the commands they
///     care about and drop them again on the way out, this subscribes to all of them once
///     and every handler opens by asking whether a run is on. That is the trade: one place
///     to look for the wiring, and an <c>if</c> in front of every reaction.
/// </summary>
public class AthMod
{
	private readonly ModServices _services;

	private IGamemode _currentGamemode;

	/// <summary>
	///     The mode a queued start is waiting to run, or null when nothing is queued. Not a
	///     bool: a start carries a mode, so remembering that one was requested is not enough
	///     or a queued Ranked start would come back as whatever happens to be selected when
	///     the race begins.
	/// </summary>
	private IGamemode _pendingGamemode;

	private AthRunner _run;
	private bool _running;

	/// <summary>
	///     Set while <see cref="EndRun" /> is on the stack. Ending a run runs the runner's
	///     end report, which raises RunFinished - and EndRun is the subscriber of exactly
	///     that event. Without the flag the teardown re-enters itself: the "stopped" toast
	///     appears doubled and the second Dispose() touches an already destroyed component.
	/// </summary>
	private bool _shuttingDown;

	public AthMod(ModServices services)
	{
		_services = services;
	}

	public static bool IsActive { get; private set; }

	/// <summary>Subscribes the mod to everything it reacts to, for the whole session.</summary>
	public void Initialize()
	{
		CommandStart.CommandTrigger += OnStartCommand;
		CommandStop.CommandTrigger += OnStopCommand;
		CommandRestart.CommandTrigger += OnRestartCommand;
		CommandSkipBroken.CommandTrigger += OnSkipBrokenCommand;
		MultiplayerApi.DisconnectedFromGame += OnDisconnected;
		RacingApi.RoundStarted += OnRoundStarted;
		_services.GameState.BecameRacing += OnBecameRacing;
	}

	public void Dispose()
	{
		CommandStart.CommandTrigger -= OnStartCommand;
		CommandStop.CommandTrigger -= OnStopCommand;
		CommandRestart.CommandTrigger -= OnRestartCommand;
		CommandSkipBroken.CommandTrigger -= OnSkipBrokenCommand;
		MultiplayerApi.DisconnectedFromGame -= OnDisconnected;
		RacingApi.RoundStarted -= OnRoundStarted;
		_services.GameState.BecameRacing -= OnBecameRacing;

		EndRun();
	}

	#region Commands

	private void OnStartCommand()
	{
		if (_running)
		{
			ToastNotification.Warn("ATH is already running");
			return;
		}

		// Read once, here: the run is committed to a mode the moment it is requested, so a
		// selection changed during the wait below does not reach into a start already made.
		IGamemode gamemode = _services.Gamemodes.Selected;

		if (!GameStateObserver.IsInOnlineLobby)
		{
			ToastNotification.Warn("ATH only runs in an online lobby");
			return;
		}

		// A run has to begin on a level that is actually loaded. Starting during the podium
		// or while the next map is still loading would have the runner rewrite a playlist
		// the game is in the middle of switching.
		if (!GameStateObserver.IsRacing)
		{
			_pendingGamemode = gamemode;
			ToastNotification.Info($"{gamemode.DisplayName} starts as soon as the level is loaded");
			Logger.LogInfo($"AthMod: Start requested while {DescribeWait()}, waiting for the race to start.");
			return;
		}

		BeginRun(gamemode);
	}

	private void OnStopCommand()
	{
		if (!_running)
		{
			if (_pendingGamemode != null)
			{
				_pendingGamemode = null;
				ToastNotification.Info("Pending start cancelled");
				return;
			}

			ToastNotification.Warn("ATH is not running");
			return;
		}

		EndRun();
	}

	private void OnRestartCommand()
	{
		if (!_running)
		{
			// Nothing to restart. A restart with the mod idle is a start.
			OnStartCommand();
			return;
		}

		// Held across the teardown below, which clears it.
		IGamemode gamemode = _currentGamemode;

		if (GameStateObserver.IsRacing)
		{
			EndRun();
			BeginRun(gamemode);
			return;
		}

		// Restarting into a podium or a loading screen would have the new run rewrite a
		// playlist the game is in the middle of switching. Wind this run down instead and
		// let OnBecameRacing pick the moment.
		// No race condition between the check and the transition: IsRacing is derived from
		// game state that only changes between frames, and nothing here yields.
		Logger.LogInfo("AthMod: Restart requested outside a running race, deferring the new run.");
		ToastNotification.Info("ATH restarts as soon as the level is loaded");
		EndRun();
		_pendingGamemode = gamemode;
	}

	private void OnSkipBrokenCommand()
	{
		if (!_running || _run.Ctx.CurrentLevel == null)
		{
			return;
		}

		_run.Ctx.CurrentLevel.LevelBroken = true;
		ChatApi.SendMessage("/fs");
	}

	#endregion

	#region Game Events

	private void OnDisconnected()
	{
		if (!_running)
		{
			return;
		}

		EndRun();
	}

	private void OnRoundStarted()
	{
		if (!_running)
		{
			return;
		}

		// ATH shows its own clock, so the lobby's time display stays off for the run.
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
	}

	private void OnBecameRacing()
	{
		if (_running || _pendingGamemode == null)
		{
			return;
		}

		IGamemode gamemode = _pendingGamemode;
		_pendingGamemode = null;
		Logger.LogInfo($"AthMod: Race is running, starting the pending {gamemode.Id} run.");
		BeginRun(gamemode);
	}

	private void OnRunFinished()
	{
		EndRun();
	}

	#endregion

	#region Run Lifecycle

	private void BeginRun(IGamemode gamemode)
	{
		if (!IsHudReady())
		{
			return;
		}

		_currentGamemode = gamemode;
		_run = new AthRunner(_services, gamemode);
		_run.RunFinished += OnRunFinished;

		_running = true;
		IsActive = true;

		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
		_services.PublishRun(_run);
		_run.StartTimer();
		ToastNotification.Success($"{gamemode.DisplayName} started");
		_run.Begin();
	}

	/// <summary>
	///     Ends the run currently in progress. Reachable three ways - /ath stop, the runner
	///     finishing on its own, and a disconnect - so it has to be safe to call twice and
	///     safe to call from inside itself.
	/// </summary>
	private void EndRun()
	{
		if (!_running || _shuttingDown)
		{
			return;
		}

		_shuttingDown = true;

		try
		{
			// Runs the end report if it has not run already. Re-enters this method through
			// RunFinished, which is what _shuttingDown is guarding.
			_run.Stop();
			ToastNotification.Info("Hunt stopped");
			_services.PublishRun(null);
			_run.RunFinished -= OnRunFinished;
			_run.StopTimer();
			_run.Dispose();
			RestoreGameHud();
		}
		finally
		{
			_run = null;
			_currentGamemode = null;
			_running = false;
			IsActive = false;
			_shuttingDown = false;
		}
	}

	/// <summary>
	///     BeginRun reaches straight into the online HUD. If that chain is not there the
	///     start would die halfway through, leaving the mod half-running. Refuse first.
	/// </summary>
	private static bool IsHudReady()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			ToastNotification.Warn("Online HUD not ready yet, try again in a moment");
			return false;
		}

		return true;
	}

	/// <summary>
	///     Hands the HUD elements ATH borrowed back to the game. The whole chain is gone when
	///     the stop was triggered by DisconnectedFromGame, so it stays optional.
	/// </summary>
	private static void RestoreGameHud()
	{
		if (PlayerManager.Instance == null || PlayerManager.Instance.currentMaster == null ||
		    PlayerManager.Instance.currentMaster.OnlineGameplayUI == null)
		{
			return;
		}

		PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = true;
	}

	private static string DescribeWait()
	{
		return !GameStateObserver.IsLevelReady
			? "the level is loading"
			: $"the lobby is in {GameStateObserver.LobbyState}";
	}

	#endregion
}
