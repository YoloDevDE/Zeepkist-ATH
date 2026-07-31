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
    /// <summary>
    ///     A level has to have eaten at least this much time before it is a candidate for
    ///     "you should have skipped this".
    /// </summary>
    private const double TimeSinkThresholdInMinutes = 5;

    /// <summary>
    ///     How often the same author has to show up before the run calls it a pattern.
    /// </summary>
    private const int HauntingAuthorMinimumLevels = 2;

    private readonly IReadOnlyList<Level> _levels;

    public RunStatistics(IReadOnlyList<Level> levels)
    {
        _levels = levels ?? new List<Level>();
    }

    /// <summary>
    ///     Total resets across all levels, i.e. how often the player started a run.
    /// </summary>
    public int TotalAttempts => _levels.Sum(level => level.Attempt);

    /// <summary>
    ///     Author times that were hit on the very first attempt.
    /// </summary>
    public int OneShotAuthorTimes => _levels.Count(level => level.AuthorTimeAcquired && level.Attempt == 1);

    /// <summary>
    ///     Levels that were skipped at the cost of a time penalty.
    /// </summary>
    public int PenaltySkipCount => _levels.Count(level => level.PenaltySkipped);

    /// <summary>
    ///     Average author time of the levels the run served up - a rough difficulty readout.
    ///     Broken levels are excluded; they were never really played.
    /// </summary>
    public double AverageAuthorTime
    {
        get
        {
            List<Level> playable = _levels.Where(level => !level.LevelBroken).ToList();
            // Average() throws on an empty sequence, and a run can legitimately end without
            // a single playable level.
            return playable.Count == 0 ? 0 : playable.Average(level => level.AuthorTime);
        }
    }

    /// <summary>
    ///     Average number of attempts spent per author time actually claimed.
    /// </summary>
    public double AverageAttemptsPerAuthorTime
    {
        get
        {
            List<Level> beaten = BeatenLevels();
            return beaten.Count == 0 ? 0 : beaten.Average(level => level.Attempt);
        }
    }

    /// <summary>
    ///     Average time spent per author time actually claimed.
    /// </summary>
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

    /// <summary>
    ///     Time spent beyond the personal best on each level - the cost of every failed run.
    /// </summary>
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

    /// <summary>
    ///     The level that swallowed the most time, provided it crossed
    ///     <see cref="TimeSinkThresholdInMinutes" />. Null when no level qualifies.
    ///     Broken levels are excluded - their time is refunded, so it was never wasted.
    /// </summary>
    public Level BiggestTimeSink =>
        _levels.Where(level => !level.LevelBroken && level.GetPlayDuration().TotalMinutes >= TimeSinkThresholdInMinutes).OrderByDescending(level => level.GetPlayDuration()).FirstOrDefault();

    /// <summary>
    ///     The claimed author time that came cheapest, by attempts first and time second.
    ///     Null when the run claimed none.
    /// </summary>
    public Level EasiestBeatenLevel => _levels.Where(level => level.AuthorTimeAcquired).OrderBy(level => level.Attempt).ThenBy(level => level.GetPlayDuration()).FirstOrDefault();

    /// <summary>
    ///     An author whose levels the player beat at least
    ///     <see cref="HauntingAuthorMinimumLevels" /> times. Levels is null or empty when
    ///     no author qualifies.
    /// </summary>
    public (string Author, List<Level> Levels) MostBeatenAuthor =>
        _levels.Where(level => level.AuthorTimeAcquired).GroupBy(level => level.Author).Where(group => group.Count() >= HauntingAuthorMinimumLevels)
               .Select(group => (Author: group.Key, Levels: group.ToList())).FirstOrDefault();

    private List<Level> BeatenLevels() => _levels.Where(level => level.AuthorTimeAcquired).ToList();
}
