using System.Collections.Generic;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     What a run did at every checkpoint it reached: when it got there, and how fast it was
///     going through. Two lists that are only ever read together and would be a bug the moment
///     they were not the same length, so they travel as one thing.
///     A missing entry is -1 rather than an exception. Both callers ask about checkpoints the
///     run may never have reached - the attempt being driven has not got there yet, and the
///     reference run may be from a level that has since been rebuilt with more of them.
/// </summary>
public class SplitSet
{
	public static readonly SplitSet Empty = new([], []);

	public SplitSet(IReadOnlyList<double> times, IReadOnlyList<double> speeds)
	{
		Times = times ?? [];
		Speeds = speeds ?? [];
	}

	public IReadOnlyList<double> Times { get; }

	public IReadOnlyList<double> Speeds { get; }

	public int Count => Times.Count;

	/// <summary>Seconds off the start at that checkpoint, or -1 where the run has none.</summary>
	public double TimeAt(int index)
	{
		return index >= 0 && index < Times.Count ? Times[index] : -1d;
	}

	/// <summary>Km/h through that checkpoint, or -1 where the run has none.</summary>
	public double SpeedAt(int index)
	{
		return index >= 0 && index < Speeds.Count ? Speeds[index] : -1d;
	}
}
