using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Gamemodes;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     The state of a single run: which levels were played, how much of the time budget is
///     left, and the scratch values the states hand to each other.
///     Two collaborators hang off it rather than living in it - <see cref="Messages" /> for
///     everything the run says, and <see cref="RunStatistics" /> for the end-of-run numbers.
/// </summary>
public class AthCtx
{
	private const int RETRIES = 3;
	private bool _previousTimeRunningLowState;

	public AthCtx(RunSettings settings)
	{
		Settings = settings;
		Duration = settings.DurationMs;
		PenaltyTimeInMilliseconds = settings.PenaltyTimeMs;
		AvaiableFreeSkips = settings.FreeSkips;
	}

	#region Run Settings

	public RunSettings Settings { get; }

	public int Duration { get; }

	public int PenaltyTimeInMilliseconds { get; }

	#endregion

	#region Run State

	public Level CurrentLevel { get; set; }

	public List<Level> Levels { get; } = [];

	public int Retries { get; set; } = RETRIES;

	public int ConsecutiveDuplicateCount { get; set; }

	public int ConsecutiveBrokenCount { get; set; }

	public int AvaiableFreeSkips { get; set; }

	public bool IsPaused { get; set; }

	#endregion

	#region Last Run Scratch State

	public LevelStatus LastRunMedalStatus { get; set; } = LevelStatus.UNKNOWN;
	public bool LastRunMedalWasNew { get; set; }
	public double LastRunTime { get; set; } = -1;

	#endregion

	#region Live Counters

	public int AuthorMedals => Levels.Count(level => level.Status == LevelStatus.AUTHOR);

	public int GoldMedals => Levels.Count(level => level.Status == LevelStatus.GOLD);

	public int Penalties => Levels.Count(level => level.Status == LevelStatus.FAILED);

	#endregion

	#region Time Budget

	public bool IsTimeRunningLow => GetRemainingTime().TotalMilliseconds <= PenaltyTimeInMilliseconds;

	public bool IsTimeAfterSkipRunningLow => GetRemainingTime().TotalMilliseconds <= 2 * PenaltyTimeInMilliseconds;

	public int GetAccumulatedPenaltyTime()
	{
		return PenaltyTimeInMilliseconds * Penalties;
	}

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

	public TimeSpan GetRemainingTimeWithoutPunishments()
	{
		return TimeSpan.FromMilliseconds(Duration - GetTotalLevelPlayDuration().TotalMilliseconds);
	}

	public bool IsTimeOver()
	{
		return GetRemainingTime() <= TimeSpan.Zero;
	}

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

		LastRunTime = -1;
		LastRunMedalStatus = LevelStatus.UNKNOWN;
		LastRunMedalWasNew = false;

		CurrentLevel.Start();
	}

	public void ResetRetries()
	{
		Retries = RETRIES;
	}

	#endregion
}
