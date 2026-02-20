using System;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     One respawn-based attempt within a LevelSession.
///     Starts at spawn and ends at respawn/finish/abort.
/// </summary>
public sealed class Attempt
{
    public Attempt(int index, DateTimeOffset startedAt)
    {
        if (index <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(index));
        }

        Index = index;
        StartedAt = startedAt;
        Result = AttemptResult.InProgress;
    }

    public int Index { get; }
    public DateTimeOffset StartedAt { get; }
    public DateTimeOffset? EndedAt { get; private set; }

    public AttemptResult Result { get; private set; }

    /// <summary>Set only if Result == Finished.</summary>
    public TimeSpan? FinishedTime { get; private set; }

    /// <summary>Optional info if aborted by logic/user.</summary>
    public string AbortReason { get; private set; }

    public void EndByRespawn(DateTimeOffset endedAt)
    {
        EnsureInProgress();
        EndedAt = endedAt;
        Result = AttemptResult.Respawned;
    }

    public void EndByFinish(DateTimeOffset endedAt, TimeSpan finishTime)
    {
        EnsureInProgress();
        EndedAt = endedAt;
        FinishedTime = finishTime;
        Result = AttemptResult.Finished;
    }

    public void EndByAbort(DateTimeOffset endedAt, string reason = null)
    {
        EnsureInProgress();
        EndedAt = endedAt;
        AbortReason = reason;
        Result = AttemptResult.Aborted;
    }

    private void EnsureInProgress()
    {
        if (Result != AttemptResult.InProgress)
        {
            throw new InvalidOperationException($"Attempt already ended (Result={Result}).");
        }
    }
}

public enum AttemptResult
{
    InProgress,
    Respawned,
    Finished,
    Aborted
}