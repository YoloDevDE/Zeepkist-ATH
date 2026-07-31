using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     The state of a single run: which levels were played, how much of the time budget is
///     left, and the scratch values the states hand to each other.
///     Two collaborators hang off it rather than living in it - <see cref="Messages" /> for
///     everything the run says, and <see cref="RunStatistics" /> for the end-of-run numbers.
/// </summary>
public class AthCtx
{
	// Constants
	private const int RETRIES = 3;
	private bool _previousTimeRunningLowState;

	/// <summary>
	///     One AthCtx exists per run - it is created in AthStateMachine.Awake(), and a fresh
	///     AthStateMachine is built for every /ath start and /ath restart.
	/// </summary>
	public AthCtx()
	{
		// Snapshot, not live reads: both values used to be read from the config on every
		// access, so opening the config mid-run and raising Duration handed out extra time.
		Duration = Plugin.Instance.MyConfig.Duration.Value * 1000;
		PenaltyTimeInMilliseconds = Plugin.Instance.MyConfig.PenaltyTime.Value * 1000;
	}

	#region Run Settings

	/// <summary>Time budget for the whole run, in milliseconds. Fixed at run start.</summary>
	public int Duration { get; }

	/// <summary>Cost of a penalty skip, in milliseconds. Fixed at run start.</summary>
	public int PenaltyTimeInMilliseconds { get; }

	#endregion

	#region Run State

	public Level CurrentLevel { get; set; }

	/// <summary>Every level the run has touched, in the order they were played.</summary>
	public List<Level> Levels { get; } = [];

	public int Retries { get; set; } = RETRIES;

	public int ConsecutiveDuplicateCount { get; set; }

	public int AvaiableFreeSkips { get; set; } = 1;

	/// <summary>
	///     True while the player has paused the run from the UI. The level clock is stopped
	///     and stays stopped across round starts until they resume.
	/// </summary>
	public bool IsPaused { get; set; }

	#endregion

	#region Last Run Scratch State

	// Reset per level in InitializingNewLevel - the medal overlay reads these while the
	// player is on the round-over screen, before the next level is loaded.
	public Level.LevelStatus LastRunMedalStatus { get; set; } = Level.LevelStatus.UNKNOWN;
	public bool LastRunMedalWasNew { get; set; }
	public double LastRunTime { get; set; } = -1;

	#endregion

	#region Live Counters

	public int AuthorMedals => Levels.Count(level => level.Status == Level.LevelStatus.AUTHOR);

	public int GoldMedals => Levels.Count(level => level.Status == Level.LevelStatus.GOLD);

	public int Penalties => Levels.Count(level => level.Status == Level.LevelStatus.FAILED);

	#endregion

	#region Time Budget

	// Fatal tier: a single further penalty skip would exhaust the budget.
	// GetRemainingTime() already has the accumulated penalties subtracted, so the
	// threshold is one penalty - adding GetAccumulatedPenaltyTime() counted them twice.
	public bool IsTimeRunningLow => GetRemainingTime().TotalMilliseconds <= PenaltyTimeInMilliseconds;

	// Warning tier: one penalty skip away from the fatal tier.
	public bool IsTimeAfterSkipRunningLow => GetRemainingTime().TotalMilliseconds <= 2 * PenaltyTimeInMilliseconds;

	public int GetAccumulatedPenaltyTime()
	{
		return PenaltyTimeInMilliseconds * Penalties;
	}

	/// <summary>
	///     Time actually spent playing. Broken levels do not count - their time is refunded.
	/// </summary>
	public TimeSpan GetTotalLevelPlayDuration()
	{
		return TimeSpan.FromMilliseconds(Levels.Where(level => !level.LevelBroken)
			.Sum(level => level.GetPlayDuration().TotalMilliseconds));
	}

	public TimeSpan GetRemainingTime()
	{
		return TimeSpan.FromMilliseconds(Duration -
		                                 (GetTotalLevelPlayDuration().TotalMilliseconds + GetAccumulatedPenaltyTime()));
	}

	/// <summary>
	///     What would be left if no penalty had ever been taken. The HUD shows this next to
	///     the penalty total so the cost of skipping stays visible.
	/// </summary>
	public TimeSpan GetRemainingTimeWithoutPunishments()
	{
		return TimeSpan.FromMilliseconds(Duration - GetTotalLevelPlayDuration().TotalMilliseconds);
	}

	public bool IsTimeOver()
	{
		return GetRemainingTime() <= TimeSpan.Zero;
	}

	/// <summary>
	///     Edge detection for the low-time warning: true only on the frame the run crosses
	///     into the fatal tier, so the caller notifies once instead of every frame.
	/// </summary>
	public bool CheckAndNotifyTimeRunningLow()
	{
		bool currentState = IsTimeRunningLow;
		bool shouldNotify = currentState && !_previousTimeRunningLowState;
		_previousTimeRunningLowState = currentState;
		return shouldNotify;
	}

	#endregion

	#region Level Management

	public void InitializingNewLevel(LevelScriptableObject levelScriptableObject)
	{
		Level level = new(levelScriptableObject);
		CurrentLevel = level;
		Levels.Add(level);

		// Per-level scratch state. Without the reset the medal overlay keeps showing
		// the previous level's run until the player crosses a finish line here.
		LastRunTime = -1;
		LastRunMedalStatus = Level.LevelStatus.UNKNOWN;
		LastRunMedalWasNew = false;

		CurrentLevel.Start();
	}

	public void ResetRetries()
	{
		Retries = RETRIES;
	}

	#endregion
}