using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using Crosstales;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Level;
using ZeepSDK.Multiplayer;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Run;

/// <summary>
///     One hunt, from the first countdown to the end report, without a state machine.
///     This is the same control flow the state machine variant spreads over twelve
///     StateAth* classes, written the way it is usually written before anybody reaches for a
///     state machine: one object, a handful of booleans saying where the run currently is,
///     and an if-chain at the top of every event handler deciding whether this event means
///     anything right now.
///     Read <see cref="_starting" /> through <see cref="_stopped" /> as the phase, and see
///     NOTES.md for what this costs against the state machine.
/// </summary>
public partial class AthRunner
{
	/// <summary>How many frames in a row the tick may throw before the run is given up on.</summary>
	private const int MaxConsecutiveTickFailures = 10;

	/// <summary>Duplicate levels drawn back to back before the run gives up.</summary>
	private const int MaxConsecutiveDuplicates = 3;

	private static readonly TimeSpan ServerMessageThrottle = TimeSpan.FromMilliseconds(1000);

	private AthLoopBehaviour _behaviour;
	private int _consecutiveTickFailures;
	private CancellationTokenSource _countdown;
	private bool _eventsSubscribed;
	private string _lastServerMessage;
	private DateTime _lastServerMessageTime = DateTime.MinValue;
	private bool _timerStarted;

	#region Phase

	// Exactly one of these is true at a time, except while an async step is in flight, when
	// all of them are false and every event is ignored. Nothing enforces that - it is a rule
	// this file keeps by hand, and the reason ClearPhase() exists.

	/// <summary>Playlist is being set up and the countdown is running.</summary>
	private bool _starting;

	/// <summary>Waiting for a level to load, with the budget and playlist checks still ahead.</summary>
	private bool _awaitingLevel;

	/// <summary>A replacement for a level that failed to load is being drawn and pushed.</summary>
	private bool _resolvingBroken;

	/// <summary>A replacement for an already played level is being drawn and pushed.</summary>
	private bool _resolvingDuplicate;

	/// <summary>The level is registered and the run is waiting for the round to start.</summary>
	private bool _levelReady;

	/// <summary>The player is driving. This is the only phase in which the clock runs.</summary>
	private bool _driving;

	/// <summary>An attempt is over and the player is on the round-over screen.</summary>
	private bool _betweenAttempts;

	/// <summary>The author time is claimed and the run waits for a respawn to move on.</summary>
	private bool _authorClaimed;

	/// <summary>The run is over. Every handler leaves immediately from here on.</summary>
	private bool _stopped;

	#endregion

	public AthRunner(ModServices services, IGamemode gamemode)
	{
		// One AthRunner per run, so this is the run's starting line: fresh context, and a
		// level pool that does not carry the exclusions of previous runs.
		Services = services;
		Gamemode = gamemode;
		Ctx = new AthCtx(gamemode.CreateSettings());
		RandomLevels = services.CreateRandomLevelService();

		GameObject host = new(nameof(AthRunner))
		{
			hideFlags = HideFlags.HideAndDontSave
		};

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<AthLoopBehaviour>();
		_behaviour.Bind(this);
	}

	public AthCtx Ctx { get; }

	/// <summary>
	///     The mode this run is being played in. Held rather than looked up, so a mode change
	///     between runs cannot rewrite the run that is already going.
	/// </summary>
	public IGamemode Gamemode { get; }

	/// <summary>Session-scoped services shared with the rest of the mod.</summary>
	public ModServices Services { get; }

	/// <summary>This run's level pool. A new run gets a new one.</summary>
	public RandomLevelService RandomLevels { get; }

	/// <summary>Shorthand for the session's playlist service.</summary>
	private PlaylistService PlaylistService => Services.Playlist;

	/// <summary>Raised once the run has finished its end report.</summary>
	public event Action RunFinished;

	#region Phase Handling

	/// <summary>
	///     Leaves whatever phase the run is in. Everything a state's Exit() would have done
	///     has to happen here, because there is no transition to hang it on any more - which
	///     is why the clock is stopped in this one place rather than at every call site that
	///     leaves the driving phase.
	/// </summary>
	private void ClearPhase()
	{
		if (_driving)
		{
			Ctx.CurrentLevel?.PauseTiming();
		}

		_starting = false;
		_awaitingLevel = false;
		_resolvingBroken = false;
		_resolvingDuplicate = false;
		_levelReady = false;
		_driving = false;
		_betweenAttempts = false;
		_authorClaimed = false;
	}

	#endregion

	#region Starting

	/// <summary>
	///     Starts the hunt: takes the round timer away from the lobby, builds the playlist,
	///     counts down and jumps to the first level.
	/// </summary>
	public async void Begin()
	{
		ClearPhase();
		_starting = true;

		try
		{
			// ATH owns the clock - the lobby round timer must not cut a level short.
			ZeepkistNetwork.CurrentLobby.RoundTime = 86400;

			if (!Ctx.Settings.RandomPlaylist)
			{
				await Task.Delay(2500);
				MultiplayerApi.UpdateServerPlaylist();
				await Task.Delay(500);
				await RunCountdown();

				// The countdown can be cut short by OnRoundEnded having already moved the run
				// on, and by the run being stopped outright. Neither is visible from here
				// other than by asking what the phase is now.
				if (!_starting)
				{
					return;
				}

				PlaylistService.SkipLevel();
				EnterAwaitingLevel();
				return;
			}

			await StartRandomPlaylist();
			await RunCountdown();

			if (!_starting)
			{
				return;
			}

			try
			{
				PlaylistService.SkipToFirstLevel();
			}
			catch (Exception ex)
			{
				Logger.LogError($"AthRunner: Failed to skip level: {ex.Message}");
				ToastNotification.Error("Error while skipping to the first level");
			}

			EnterAwaitingLevel();
		}
		catch (Exception ex)
		{
			// async void - nothing above us can catch this.
			Logger.LogError($"AthRunner.Begin failed: {ex.Message}\nStack trace: {ex.StackTrace}");
			ToastNotification.Error("Something went wrong while starting the hunt");
		}
	}

	private async Task StartRandomPlaylist()
	{
		int retryCount = 3;

		while (retryCount > 0)
		{
			try
			{
				OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();
				PlaylistService.StartNewPlaylist(level);
				return;
			}
			catch (Exception ex)
			{
				retryCount--;
				Logger.LogError($"AthRunner: Failed to start playlist: {ex.Message}");

				if (retryCount <= 0)
				{
					// Carry on regardless - the skip below may still land on something.
					ToastNotification.Error("Failed to start playlist after multiple attempts");
					return;
				}

				await Task.Delay(500);
			}
		}
	}

	private async Task RunCountdown()
	{
		_countdown = new CancellationTokenSource();

		try
		{
			for (int i = 5; i >= 1; i--)
			{
				await Task.Delay(1000, _countdown.Token);
			}
		}
		catch (OperationCanceledException)
		{
			// The run was stopped while counting down. Nothing to clean up beyond the
			// finally block.
		}
		finally
		{
			// Null first: Stop() may still call Cancel(), and that throws on a disposed source.
			CancellationTokenSource countdown = _countdown;
			_countdown = null;
			countdown?.Dispose();
		}
	}

	#endregion

	#region Level Cycle

	/// <summary>
	///     Waits for the next level to load. The budget and playlist checks happen when it
	///     does, not here, because both can change while the level is still loading.
	/// </summary>
	private void EnterAwaitingLevel()
	{
		ClearPhase();
		_awaitingLevel = true;
	}

	/// <summary>
	///     Works out what the level that just loaded actually is: the one the playlist asked
	///     for, a fallback because that one is broken, or one already played.
	/// </summary>
	private async void ProcessLoadedLevel()
	{
		// No phase: nothing here reacts to events, and leaving a phase set would let one
		// through in the middle of the check below.
		ClearPhase();

		try
		{
			if (LevelApi.CurrentLevel == null)
			{
				Logger.LogError("AthRunner: Failed to create Level object");
				await Task.Delay(100);

				if (_stopped)
				{
					return;
				}

				ProcessLoadedLevel();
				return;
			}

			Level currentLevel = new(LevelApi.CurrentLevel);

			if (IsBrokenLevel(currentLevel))
			{
				ResolveBrokenLevel();
				return;
			}

			if (IsDuplicateLevel(currentLevel))
			{
				ResolveDuplicateLevel();
				return;
			}

			// A level we actually keep ends the duplicate streak.
			Ctx.ConsecutiveDuplicateCount = 0;
			StartLevelFirstTime();
		}
		catch (Exception ex)
		{
			// Ending the run is a heavy answer to a failure here, so it must not be a silent
			// one: without the toast this looked like the run stopping itself for no reason.
			Logger.LogError(
				$"AthRunner: Unhandled exception while processing the level, ending the run: {ex.Message}\nStack trace: {ex.StackTrace}");
			ToastNotification.Error("Could not process the level - the hunt was stopped");
			Stop();
		}
	}

	/// <summary>
	///     True when the level the game actually loaded is not the one the playlist says is
	///     current. That is what a level failing to load looks like from here: the lobby moves
	///     on, the scene does not.
	/// </summary>
	private static bool IsBrokenLevel(Level level)
	{
		List<OnlineZeeplevel> playlist = ZeepkistNetwork.CurrentLobby.Playlist;
		int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;

		if (currentIndex < 0 || currentIndex >= playlist.Count)
		{
			// Being out of step with the lobby is not the same as the level being broken.
			// There is nothing to compare against, so trust what was loaded.
			Logger.LogWarning(
				$"AthRunner: Playlist index {currentIndex} is outside a playlist of {playlist.Count} entries, accepting '{level.Name}' as loaded.");
			return false;
		}

		return !playlist[currentIndex].UID.Equals(level.LevelUid);
	}

	private bool IsDuplicateLevel(Level level)
	{
		return Ctx.Settings.RejectDuplicateLevels && Ctx.Levels.Contains(level);
	}

	/// <summary>
	///     Swaps the entry that failed to load for a fresh one and reloads. Ctx.CurrentLevel
	///     is deliberately not the broken level: only <see cref="StartLevelFirstTime" />
	///     writes to the context, so on this path it still holds the previous level.
	/// </summary>
	private async void ResolveBrokenLevel()
	{
		ClearPhase();
		_resolvingBroken = true;

		OnlineZeeplevel brokenLevel;

		try
		{
			int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
			brokenLevel = ZeepkistNetwork.CurrentLobby.Playlist[currentIndex];
		}
		catch (Exception ex)
		{
			Logger.LogError($"AthRunner: Could not read the broken playlist entry: {ex.Message}");
			Stop();
			return;
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
			Logger.LogError($"AthRunner: Could not draw a replacement for the broken level: {ex.Message}");
			ToastNotification.Error("Could not find a replacement for the broken level");
			Stop();
		}
	}

	private async void ResolveDuplicateLevel()
	{
		ClearPhase();
		_resolvingDuplicate = true;
		Ctx.ConsecutiveDuplicateCount++;

		if (Ctx.ConsecutiveDuplicateCount >= MaxConsecutiveDuplicates)
		{
			Logger.LogWarning(
				$"AthRunner: Duplicate limit reached after {Ctx.ConsecutiveDuplicateCount} retries. Ending run.");
			Stop();
			return;
		}

		try
		{
			OnlineZeeplevel newLevel = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.AddLevelToCurrentPlaylist(newLevel);
			PlaylistService.SkipToNextLevel();
		}
		catch (Exception ex)
		{
			// async void - nothing above us can catch this.
			Logger.LogError($"AthRunner: Could not draw another level: {ex.Message}");
			ToastNotification.Error("Could not find another level to play");
			Stop();
		}
	}

	/// <summary>Registers the loaded level with the run and waits for the round to start.</summary>
	private void StartLevelFirstTime()
	{
		ClearPhase();
		_levelReady = true;

		try
		{
			Ctx.InitializingNewLevel(LevelApi.CurrentLevel);
			SetServerMessage(true);
		}
		catch (Exception e)
		{
			Logger.LogError($"AthRunner: Failed to start level: {e.Message}\nStack trace: {e.StackTrace}");
			Stop();
		}
	}

	/// <summary>
	///     Pre-loads the level after the current one so the playlist never runs dry. Started
	///     without awaiting - the run continues either way - so it has to swallow and report
	///     its own failures. Unhandled, they would end up in an unobserved Task and the
	///     playlist would simply be empty at the end with nothing in the log to explain it.
	/// </summary>
	private async Task PrefetchNextLevelAsync()
	{
		if (!Ctx.Settings.RandomPlaylist)
		{
			return;
		}

		try
		{
			Logger.LogInfo("Adding Random Level");
			OnlineZeeplevel level = await RandomLevels.DrawRandomLevelAsync();
			PlaylistService.AddLevelToCurrentPlaylist(level);
		}
		catch (Exception e)
		{
			Logger.LogError($"AthRunner: Could not pre-load the next level: {e.Message}");
			ToastNotification.Warn("Could not load the next level - the playlist may run out");
		}
	}

	#endregion

	#region Attempts

	/// <summary>Hands the clock to the player. The only place the run starts billing time.</summary>
	private void BeginDriving()
	{
		ClearPhase();
		_driving = true;

		// A run resumed while the player paused ATH must not restart the clock.
		if (!Ctx.IsPaused)
		{
			Ctx.CurrentLevel.ResumeTiming();
		}

		DrivingTick();
	}

	/// <summary>The round-over screen: clock stopped, waiting for the next attempt or a skip.</summary>
	private void BeginPause()
	{
		ClearPhase();
		_betweenAttempts = true;
		Ctx.ResetRetries();
		SetServerMessage(true);
	}

	/// <summary>
	///     The author time is in. The level is closed here rather than at the skip, because
	///     the time between claiming it and respawning is not the player's to pay for.
	/// </summary>
	private void BeginAuthorWait()
	{
		ClearPhase();
		_authorClaimed = true;
		Ctx.CurrentLevel.Stop();
		ToastNotification.Info("Author time claimed!<br>[Respawn to continue]");
		SetServerMessage(true);
	}

	private void DrivingTick()
	{
		if (Ctx.IsTimeOver())
		{
			Stop();
			return;
		}

		SetServerMessage(false);

		if (Ctx.CheckAndNotifyTimeRunningLow())
		{
			ToastNotification.Info("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", 10f);
		}
	}

	private static int GetMedalRank(Level.LevelStatus status)
	{
		return status switch
		{
			Level.LevelStatus.AUTHOR => 2, Level.LevelStatus.GOLD => 1, _ => 0
		};
	}

	private static Level.LevelStatus ResolveRunMedalStatus(float runTime, Level level)
	{
		if (runTime <= level.AuthorTime)
		{
			return Level.LevelStatus.AUTHOR;
		}

		if (runTime <= level.GoldTime)
		{
			return Level.LevelStatus.GOLD;
		}

		return Level.LevelStatus.UNKNOWN;
	}

	#endregion

	#region Skipping

	/// <summary>Works out what leaving this level costs, charges it, and moves on.</summary>
	private void EvaluateSkip()
	{
		ClearPhase();

		if (Ctx.CurrentLevel.LevelBroken)
		{
			ToastNotification.Warn("'Broken-Skip' used - spent time refunded");
		}
		else if (Ctx.CurrentLevel.GoldMedalAcquired)
		{
			ToastNotification.Success("'Gold-Skip' used");
		}
		else if (Ctx.AvaiableFreeSkips > 0)
		{
			Ctx.AvaiableFreeSkips -= 1;
			Ctx.CurrentLevel.FreeSkipped = true;
			ToastNotification.Success("'Free-Skip' used");
		}
		else if (Ctx.IsTimeRunningLow)
		{
			ToastNotification.Error("Well.. I tried to warn you.. Hunt is over once the level is loaded.", 10f);
		}
		else
		{
			ToastNotification.Error("'Penalty-Skip' used");
		}

		Ctx.CurrentLevel.Skipped = true;
		Ctx.CurrentLevel.Stop();
		EnterAwaitingLevel();
	}

	#endregion

	#region Stopping

	/// <summary>
	///     Ends the run and shows the report. Safe to call from anywhere and more than once -
	///     the run stops itself when the budget is gone, and the mod stops it from outside.
	/// </summary>
	public void Stop()
	{
		if (_stopped)
		{
			return;
		}

		ClearPhase();
		_stopped = true;
		_countdown?.Cancel();

		try
		{
			// Null when the run is stopped before the first level was ever loaded.
			Ctx.CurrentLevel?.Stop();

			// A snapshot, taken here because everything below this point tears the run down.
			Services.Results.Show(RunReportView.From(Ctx));
			SetServerMessage(true);
			SavePlaylistIfConfigured();
		}
		catch (Exception e)
		{
			Logger.LogError(e);
		}
		finally
		{
			// Last, not second. RunFinished hands control to AthMod, which tears this runner
			// down and destroys its GameObject - everything above has to have happened by
			// then. In the finally block so that a failed end summary still shuts the mod
			// down instead of leaving it half-running.
			try
			{
				RunFinished?.Invoke();
			}
			catch (Exception e)
			{
				// We may be inside a chat command handler; letting this escape helps nobody.
				Logger.LogError(e);
			}
		}
	}

	private void SavePlaylistIfConfigured()
	{
		if (!Plugin.Instance.MyConfig.SavePlaylistOnRunEnd.Value)
		{
			return;
		}

		// The last two entries are the level that was running and the one already queued
		// behind it - neither was played, so they stay out of the saved run.
		int playedCount = Math.Max(0, ZeepkistNetwork.CurrentLobby.Playlist.Count - 2);

		if (playedCount == 0)
		{
			Logger.LogInfo("AthRunner: Run too short to save a playlist, skipping.");
			return;
		}

		string playlistName =
			$"ATH-RUN-{DateTime.Now.ToString($"yyyy-MM-dd_HH-mm-ss_{Ctx.AuthorMedals}_{Ctx.GoldMedals}_{Ctx.Penalties}")}";
		PlaylistSaveJSON playlistSaveFile = new();
		playlistSaveFile.name = playlistName;
		playlistSaveFile.levels = ZeepkistNetwork.CurrentLobby.Playlist.GetRange(0, playedCount);
		playlistSaveFile.roundLength = 420;
		playlistSaveFile.amountOfLevels = playedCount;
		playlistSaveFile.CreateEditor().Save();
	}

	#endregion

	#region Pausing

	/// <summary>Stops the run clock until <see cref="ResumeRun" />.</summary>
	public void PauseRun()
	{
		if (Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = true;
		Ctx.CurrentLevel?.PauseTiming();
		Logger.LogInfo("AthRunner: Run paused.");
	}

	/// <summary>Starts the clock again, but only if a level is actually being played.</summary>
	public void ResumeRun()
	{
		if (!Ctx.IsPaused)
		{
			return;
		}

		Ctx.IsPaused = false;

		if (_driving)
		{
			Ctx.CurrentLevel?.ResumeTiming();
		}

		Logger.LogInfo("AthRunner: Run resumed.");
	}

	#endregion

	#region Event Handling

	/// <summary>
	///     Runs a game event handler without letting an exception escape. These run inside
	///     ZeepSDK's event dispatch, which other mods subscribe to as well - an exception
	///     escaping here is not ours alone to lose. Returns false when the handler threw.
	/// </summary>
	private bool TryHandle(string eventName, Action handler)
	{
		if (_stopped)
		{
			return true;
		}

		try
		{
			handler();
			return true;
		}
		catch (Exception e)
		{
			Logger.LogError($"AthRunner: {eventName} failed: {e.Message}\n{e.StackTrace}");
			return false;
		}
	}

	public void OnAthTimerTick()
	{
		if (TryHandle(nameof(OnAthTimerTick), HandleTimerTick))
		{
			_consecutiveTickFailures = 0;
			return;
		}

		// The tick runs every frame: a phase that keeps throwing would spam the log forever
		// while the run silently stops working. Tolerate a hiccup - a single null reference
		// during a level transition should not end an hour-long run - but not a pattern.
		_consecutiveTickFailures++;

		if (_consecutiveTickFailures < MaxConsecutiveTickFailures)
		{
			return;
		}

		Logger.LogError($"AthRunner: Tick failed {MaxConsecutiveTickFailures} frames in a row, stopping the run.");
		_consecutiveTickFailures = 0;
		StopTimer();

		try
		{
			Stop();
		}
		catch (Exception e)
		{
			Logger.LogError($"AthRunner: Could not stop the run after repeated tick failures: {e.Message}");
		}
	}

	private void HandleTimerTick()
	{
		if (_driving)
		{
			DrivingTick();
			return;
		}

		if (_betweenAttempts || _authorClaimed)
		{
			SetServerMessage(true);
		}
	}

	private void OnRoundStarted()
	{
		TryHandle(nameof(OnRoundStarted), HandleRoundStarted);
	}

	private void HandleRoundStarted()
	{
		if (_levelReady)
		{
			_ = PrefetchNextLevelAsync();
			BeginDriving();
			return;
		}

		if (_driving)
		{
			// Still the same level, so this is a retry rather than a new attempt at a new
			// track - the clock is already running and must not be restarted.
			Ctx.CurrentLevel.Attempt++;
			DrivingTick();
			return;
		}

		if (_betweenAttempts)
		{
			BeginDriving();
		}
	}

	private void OnRoundEnded()
	{
		TryHandle(nameof(OnRoundEnded), HandleRoundEnded);
	}

	private void HandleRoundEnded()
	{
		if (_starting)
		{
			// The lobby moved on before the countdown was done. Take the level it moved to.
			EnterAwaitingLevel();
			return;
		}

		if (_authorClaimed)
		{
			EnterAwaitingLevel();
			return;
		}

		if (_driving || _betweenAttempts)
		{
			EvaluateSkip();
		}
	}

	private void OnLevelLoaded()
	{
		TryHandle(nameof(OnLevelLoaded), HandleLevelLoaded);
	}

	private void HandleLevelLoaded()
	{
		// A replacement level is not a step forward in the run - it is a second attempt at
		// the same step - so it skips the budget and playlist checks below. Getting this
		// wrong ends a run early on a lobby whose playlist happens to be short.
		if (_resolvingBroken || _resolvingDuplicate)
		{
			ProcessLoadedLevel();
			return;
		}

		if (!_awaitingLevel)
		{
			return;
		}

		if (!Ctx.IsTimeOver() &&
		    (Ctx.Settings.RandomPlaylist || Ctx.Levels.Count < ZeepkistNetwork.CurrentLobby.Playlist.Count))
		{
			ProcessLoadedLevel();
			return;
		}

		Stop();
	}

	private void OnCrossedFinishLine(float time)
	{
		TryHandle(nameof(OnCrossedFinishLine), HandleCrossedFinishLine);
	}

	private void HandleCrossedFinishLine()
	{
		if (!_driving)
		{
			return;
		}

		ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
		PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
		Level currentLevel = Ctx.CurrentLevel;

		if (currentResult == null)
		{
			BeginPause();
			return;
		}

		Level.LevelStatus previousStatus = currentLevel.Status;

		currentLevel.PersonalBestTime = currentResult.Time;
		Level.LevelStatus runMedalStatus = ResolveRunMedalStatus(currentResult.Time, currentLevel);

		Ctx.LastRunTime = currentResult.Time;
		Ctx.LastRunMedalStatus = runMedalStatus;
		Ctx.LastRunMedalWasNew = GetMedalRank(runMedalStatus) > GetMedalRank(previousStatus);

		bool wasGoldMedalAcquiredBeforeRun = GetMedalRank(previousStatus) >= GetMedalRank(Level.LevelStatus.GOLD);

		if (currentLevel.Status == Level.LevelStatus.AUTHOR)
		{
			BeginAuthorWait();
			return;
		}

		if (runMedalStatus == Level.LevelStatus.GOLD && !wasGoldMedalAcquiredBeforeRun)
		{
			ToastNotification.Info("Gold medal claimed!<br>You can now skip without penalty");
		}

		BeginPause();
	}

	private void OnPlayerSpawned()
	{
		TryHandle(nameof(OnPlayerSpawned), HandlePlayerSpawned);
	}

	private void HandlePlayerSpawned()
	{
		if (_authorClaimed)
		{
			PlaylistService.SkipLevel();
		}
	}

	/// <summary>
	///     Photo mode is treated as "the player is driving again", because RoundStarted and
	///     this are the only two ways back into a run from the pause screen.
	///     It is only that during a race, though. Entering photo mode on the podium or while
	///     the next level loads used to restart the run clock and bill the player for time
	///     they spent looking at a screenshot.
	/// </summary>
	private void OnPhotoModeEntered()
	{
		TryHandle(nameof(OnPhotoModeEntered), HandlePhotoModeEntered);
	}

	private void HandlePhotoModeEntered()
	{
		if (!_betweenAttempts)
		{
			return;
		}

		if (!GameStateObserver.IsRacing)
		{
			Logger.LogInfo("AthRunner: Photo mode entered outside a running race, keeping the clock paused.");
			return;
		}

		BeginDriving();
	}

	/// <summary>
	///     Counted rather than dispatched: no phase cares that a crash happened, the level
	///     stats panel just wants the tally.
	/// </summary>
	private void OnCrashed(CrashReason reason)
	{
		Ctx.CurrentLevel?.RegisterCrash();
	}

	/// <summary>Fires once per wheel, so a bad landing can add four. Same reasoning as above.</summary>
	private void OnWheelBroken()
	{
		Ctx.CurrentLevel?.RegisterWheelLost();
	}

	private void SubscribeEvents()
	{
		if (_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted += OnRoundStarted;
		RacingApi.RoundEnded += OnRoundEnded;
		RacingApi.PlayerSpawned += OnPlayerSpawned;
		RacingApi.CrossedFinishLine += OnCrossedFinishLine;
		RacingApi.LevelLoaded += OnLevelLoaded;
		RacingApi.Crashed += OnCrashed;
		RacingApi.WheelBroken += OnWheelBroken;
		PhotoModeApi.PhotoModeEntered += OnPhotoModeEntered;

		_eventsSubscribed = true;
	}

	private void UnsubscribeEvents()
	{
		if (!_eventsSubscribed)
		{
			return;
		}

		RacingApi.RoundStarted -= OnRoundStarted;
		RacingApi.RoundEnded -= OnRoundEnded;
		RacingApi.PlayerSpawned -= OnPlayerSpawned;
		RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
		RacingApi.LevelLoaded -= OnLevelLoaded;
		RacingApi.Crashed -= OnCrashed;
		RacingApi.WheelBroken -= OnWheelBroken;
		PhotoModeApi.PhotoModeEntered -= OnPhotoModeEntered;

		_eventsSubscribed = false;
	}

	#endregion

	#region Lifecycle

	public void StartTimer()
	{
		if (_timerStarted)
		{
			return;
		}

		SubscribeEvents();
		_timerStarted = true;
	}

	public void StopTimer()
	{
		if (!_timerStarted)
		{
			return;
		}

		UnsubscribeEvents();
		_timerStarted = false;
	}

	public void Dispose()
	{
		StopTimer();

		// Unity's overloaded == reports a destroyed object as null, so this covers both
		// "already disposed" and "the GameObject went away underneath us".
		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}

	#endregion

	#region Server Message

	/// <summary>
	///     Refreshes the run HUD. Either renders it into the game's server message area or
	///     hands a snapshot to the in-game window, depending on the config.
	/// </summary>
	public void SetServerMessage(bool paused)
	{
		// No level yet means /ath start followed straight by /ath stop - there is nothing to
		// render and every CurrentLevel access below would throw.
		if (Ctx.CurrentLevel == null)
		{
			return;
		}

		// The window reads the run directly every frame, so nothing has to be pushed to it.
		if (Plugin.Instance.MyConfig.InGameHud.Value)
		{
			return;
		}

		var colors = new
		{
			State = paused ? "#999999" : "#42b336", TimeLeft = paused
				? "#999999"
				: Ctx.IsTimeRunningLow
					? "#bf3939"
					: Ctx.IsTimeAfterSkipRunningLow
						? "#b3b300"
						: "#42b336",
			Author = ColorDefinitions.Author.CTToHexRGB(), Default = "#e6e6e6", AuthorSkip = "#e600e6",
			GoldSkip = "#FFD600", FreeSkip = "#00ffff", EndRunSkip = "#0f0f0f", PenaltySkip = "#bf3939",
			Section = "#ffd4a6"
		};

		string skipText = Ctx.CurrentLevel.AuthorTimeAcquired
			? $"<color={colors.AuthorSkip}>Author Skip</color>"
			: Ctx.CurrentLevel.GoldMedalAcquired
				? $"<color={colors.GoldSkip}>Gold Skip</color>"
				: Ctx.AvaiableFreeSkips > 0
					? $"<color={colors.FreeSkip}>Free Skip ({Ctx.AvaiableFreeSkips}x left)</color>"
					: Ctx.IsTimeRunningLow
						? $"<color={colors.EndRunSkip}><sprite=\"Zeepkist\" name=\"Skull\"> FATAL SKIP <sprite=\"Zeepkist\" name=\"Skull\"></color>"
						: $"<color={colors.PenaltySkip}>Penalty Skip!</color>";

		string punishmentText = Ctx.Penalties == 0
			? ""
			: $"(<color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds)}</color> - <color=#ff4a4a>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds * Ctx.Penalties).ToFormattedString()}</color>)";

		string message =
			$"<size=\"20%\"><align=left><b><color=#{colors.Author}><uppercase>Author-Time-Hunting</uppercase></color></b><br>" +
			$"<color={colors.Section}><b>=== Run Settings ===</b></color><br>" +
			$"<color={colors.Default}>Duration      : {TimeSpan.FromMilliseconds(Ctx.Duration).ToFormattedString()}</color><br>" +
			$"<color={colors.Default}>Skip Penalty  : <color={colors.PenaltySkip}>{TimeSpan.FromMilliseconds(Ctx.PenaltyTimeInMilliseconds).ToFormattedString()}</color></color><br>" +
			$"<color={colors.Section}><b>=== Current Run ===</b></color><br>" +
			$"<color={colors.Default}>State         : <color={colors.State}>{(paused ? "PAUSED" : "ACTIVE")}</color></color><br>" +
			$"<color={colors.Default}>Time Left     : <color={colors.TimeLeft}>{TimeFormatter.FormatDuration((int)Ctx.GetRemainingTime().TotalMilliseconds)}</color> {punishmentText}</color><br>" +
			$"<color={colors.Default}>AT/Gold/Skips : <color={colors.AuthorSkip}>{Ctx.AuthorMedals}</color><color={colors.Default}>/</color><color={colors.GoldSkip}>{Ctx.GoldMedals}</color><color={colors.Default}>/</color><color={colors.PenaltySkip}>{Ctx.Penalties}</color></color><br>" +
			$"<color={colors.Section}><b>=== Current Level ===</b></color><br>" +
			$"<color={colors.Default}>Level Time    : <color={colors.State}>{TimeFormatter.FormatDuration((int)Ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)}</color></color><br>" +
			$"<color={colors.Default}>Skip Type     : {skipText}</color><br>" +
			$"<color={colors.Default}>Attempt       : {Ctx.CurrentLevel.Attempt}</color><br>" + "</align></size>";

		DateTime now = DateTime.UtcNow;

		if (message == _lastServerMessage && now - _lastServerMessageTime < ServerMessageThrottle)
		{
			return;
		}

		_lastServerMessage = message;
		_lastServerMessageTime = now;
		PlayerManager.Instance.currentMaster.OnlineGameplayUI.serverMessageText.text = message;
	}

	#endregion
}
