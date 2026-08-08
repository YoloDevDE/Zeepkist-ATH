using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.States.Ath;

/// <summary>
///     The numbers behind the end-of-run summary, derived purely from the levels a run
///     played. Holds no state of its own and never mutates the list it reads, so it can be
///     built ad hoc wherever it is needed.
///     Live counters that the HUD needs every frame (author medals, penalties, remaining
///     time) deliberately stay on <see cref="AthCtx" /> - those are run state, not summary.
/// </summary>
public class RunStatistics
{
	private const double _timeSinkThresholdInMinutes = 5;

	private const int _hauntingAuthorMinimumLevels = 2;

	private readonly IReadOnlyList<Level> _levels;

	public RunStatistics(IReadOnlyList<Level> levels)
	{
		_levels = levels ?? new List<Level>();
	}

	public int TotalAttempts => _levels.Sum(level => level.Attempt);

	public int OneShotAuthorTimes => _levels.Count(level => level.AuthorTimeAcquired && level.Attempt == 1);

	public int PenaltySkipCount => _levels.Count(level => level.PenaltySkipped);

	public double AverageAuthorTime
	{
		get
		{
			List<Level> playable = _levels.Where(level => !level.LevelBroken).ToList();
			return playable.Count == 0 ? 0 : playable.Average(level => level.AuthorTime);
		}
	}

	public double AverageAttemptsPerAuthorTime
	{
		get
		{
			List<Level> beaten = BeatenLevels();
			return beaten.Count == 0 ? 0 : beaten.Average(level => level.Attempt);
		}
	}

	public TimeSpan AverageTimePerAuthorTime
	{
		get
		{
			List<Level> beaten = BeatenLevels();

			if (beaten.Count == 0)
			{
				return TimeSpan.Zero;
			}

			return TimeSpan.FromTicks((long)beaten.Average(level => level.GetPlayDuration().Ticks));
		}
	}

	public TimeSpan TotalTimeWasted
	{
		get
		{
			TimeSpan total = TimeSpan.Zero;

			foreach (Level level in _levels)
			{
				total = total.Add(level.TimeWasted);
			}

			return total;
		}
	}

	public Level BiggestTimeSink =>
		_levels.Where(level => !level.LevelBroken && level.GetPlayDuration().TotalMinutes >= _timeSinkThresholdInMinutes)
			.OrderByDescending(level => level.GetPlayDuration()).FirstOrDefault();

	public Level EasiestBeatenLevel => _levels.Where(level => level.AuthorTimeAcquired).OrderBy(level => level.Attempt)
		.ThenBy(level => level.GetPlayDuration()).FirstOrDefault();

	public (string Author, List<Level> Levels) MostBeatenAuthor =>
		_levels.Where(level => level.AuthorTimeAcquired).GroupBy(level => level.Author)
			.Where(group => group.Count() >= _hauntingAuthorMinimumLevels)
			.Select(group => (Author: group.Key, Levels: group.ToList())).FirstOrDefault();

	private List<Level> BeatenLevels()
	{
		return _levels.Where(level => level.AuthorTimeAcquired).ToList();
	}
}
