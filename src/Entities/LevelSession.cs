using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorTimeHunting.Entities;

public sealed class LevelSession
{
    private readonly List<Attempt> _attempts = new List<Attempt>();

    public LevelSession(LevelInfo level, CompletionCriteria criteria)
    {
        Level = level ?? throw new ArgumentNullException(nameof(level));
        Criteria = criteria ?? throw new ArgumentNullException(nameof(criteria));
    }

    public LevelInfo Level { get; }
    public CompletionCriteria Criteria { get; }

    public LevelSessionStatus Status { get; private set; } = LevelSessionStatus.NotStarted;

    public DateTimeOffset? StartedAt { get; private set; }
    public DateTimeOffset? EndedAt { get; private set; }

    public IReadOnlyList<Attempt> Attempts => _attempts;

    public Attempt CurrentAttempt { get; private set; }

    public int AttemptCount => _attempts.Count;

    /// <summary>Best (lowest) finished time across all attempts (if any).</summary>
    public TimeSpan? BestFinishedTime =>
        _attempts
            .Where(a => a.Result == AttemptResult.Finished && a.FinishedTime.HasValue)
            .Select(a => a.FinishedTime)
            .Min();

    public bool MeetsCriteria =>
        // // If we never finished, we can't meet criteria.
        // TimeSpan? best = BestFinishedTime;
        // if (!best.HasValue)
        // {
        //     return false;
        // }
        //
        // // If there's a target time, require best <= target.
        // if (Criteria.TargetTime.HasValue && best.Value > Criteria.TargetTime.Value)
        // {
        //     return false;
        // }
        //
        // // If there's a max attempts rule, enforce it.
        // if (Criteria.MaxAttempts.HasValue && AttemptCount > Criteria.MaxAttempts.Value)
        // {
        //     return false;
        // }
        true;

    public void Start()
    {
        if (Status == LevelSessionStatus.Aborted)
        {
            throw new InvalidOperationException("Cannot Start() an aborted LevelSession. Create a new session.");
        }

        if (Status != LevelSessionStatus.NotStarted)
        {
            throw new InvalidOperationException($"LevelSession already started (Status={Status}).");
        }

        StartedAt = DateTimeOffset.UtcNow;
        Status = LevelSessionStatus.Running;
    }

    /// <summary>
    ///     Call this when the player spawns/respawns and a new attempt begins.
    /// </summary>
    public Attempt BeginAttempt()
    {
        EnsureRunning();

        if (CurrentAttempt != null && CurrentAttempt.Result == AttemptResult.InProgress)
        {
            throw new InvalidOperationException("Cannot begin a new attempt while the current attempt is still InProgress.");
        }

        CurrentAttempt = new Attempt(_attempts.Count + 1, DateTimeOffset.UtcNow);
        _attempts.Add(CurrentAttempt);
        return CurrentAttempt;
    }

    /// <summary>
    ///     Call this when the player respawns (ends the current attempt as "Respawned").
    /// </summary>
    public void EndAttemptByRespawn()
    {
        EnsureRunning();
        EnsureCurrentAttempt();

        CurrentAttempt.EndByRespawn(DateTimeOffset.UtcNow);
        CurrentAttempt = null;
    }

    /// <summary>
    ///     Call this when the player finishes the level successfully within an attempt.
    /// </summary>
    public void EndAttemptByFinish(TimeSpan finishTime)
    {
        EnsureRunning();
        EnsureCurrentAttempt();

        CurrentAttempt.EndByFinish(DateTimeOffset.UtcNow, finishTime);

        // Session ends when the level is finished; whether it "Completed" or "Failed"
        // depends on the criteria.
        EndedAt = DateTimeOffset.UtcNow;
        Status = MeetsCriteria ? LevelSessionStatus.Completed : LevelSessionStatus.Failed;

        CurrentAttempt = null;
    }

    public void Abort(string reason = null)
    {
        if (Status == LevelSessionStatus.Aborted)
        {
            return;
        }

        // Close current attempt if it’s still running.
        if (CurrentAttempt != null && CurrentAttempt.Result == AttemptResult.InProgress)
        {
            CurrentAttempt.EndByAbort(DateTimeOffset.UtcNow, reason);
            CurrentAttempt = null;
        }

        EndedAt = DateTimeOffset.UtcNow;
        Status = LevelSessionStatus.Aborted;
    }

    private void EnsureRunning()
    {
        if (Status != LevelSessionStatus.Running)
        {
            throw new InvalidOperationException($"LevelSession is not running (Status={Status}). Call Start() first.");
        }
    }

    private void EnsureCurrentAttempt()
    {
        if (CurrentAttempt == null)
        {
            throw new InvalidOperationException("No current attempt. Call BeginAttempt() first.");
        }

        if (CurrentAttempt.Result != AttemptResult.InProgress)
        {
            throw new InvalidOperationException($"Current attempt is not InProgress (Result={CurrentAttempt.Result}).");
        }
    }
}

public class CompletionCriteria { }

public class LevelInfo { }

public enum LevelSessionStatus
{
    NotStarted,
    Running,
    Completed,
    Failed,
    Aborted
}