using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     The split list as text: one row per checkpoint, in order, and the finish under them.
///     <code>
///     1   00:04.221   -0.113   78
///     2   00:09.874   +0.042   64
///     3   --:--.---
///     F   --:--.---
///     </code>
///     The game already compares a checkpoint against the best run and says so for one second,
///     in a popup that is gone before the next corner. What it never shows is the run so far, and
///     that is the thing worth looking at: one checkpoint lost is a mistake, three in a row is
///     the wrong line. So the rows stay.
///     The list is always as long as the level has checkpoints, plus the finish, from the moment
///     the lights go out. Rows are filled in as they are driven rather than appended, because a
///     row appearing pushes every row under it down - and the one being read is always the last
///     one written.
///     No colours here and nothing from Unity: this is arithmetic on two lists of doubles, which
///     is what makes it something a test can state exactly. What green means is
///     <see cref="Screens.SplitsOverlay" />'s business.
/// </summary>
public static class SplitsTable
{
	public const string NoTime = "--:--.---";

	private const string _finishLabel = "F";

	public static IReadOnlyList<SplitRow> Build(int checkpoints, SplitSet run, SplitSet best, double finishTime,
		double bestTime)
	{
		SplitSet driven = run ?? SplitSet.Empty;
		SplitSet reference = best ?? SplitSet.Empty;

		List<SplitRow> rows = [];

		for (int i = 0; i < checkpoints; i++)
		{
			rows.Add(Checkpoint(i, driven, reference));
		}

		rows.Add(Finish(finishTime, bestTime));

		return rows;
	}

	private static SplitRow Checkpoint(int index, SplitSet run, SplitSet best)
	{
		double time = run.TimeAt(index);

		if (time < 0d)
		{
			return Waiting(UiNumbers.Text(index + 1));
		}

		double speed = run.SpeedAt(index);

		return new SplitRow(UiNumbers.Text(index + 1), TimeFormatter.FormatTime(time), Gap(time, best.TimeAt(index)),
			Pace(time, best.TimeAt(index)), Speed(speed), SpeedPace(speed, best.SpeedAt(index)));
	}

	/// <summary>
	///     The run as a whole, measured against the best time rather than against a split - the
	///     finish line is not a checkpoint and the game keeps no split for it.
	/// </summary>
	private static SplitRow Finish(double finishTime, double bestTime)
	{
		if (finishTime < 0d)
		{
			return Waiting(_finishLabel);
		}

		return new SplitRow(_finishLabel, TimeFormatter.FormatTime(finishTime), Gap(finishTime, bestTime),
			Pace(finishTime, bestTime), "", SplitPace.Unknown);
	}

	private static SplitRow Waiting(string label)
	{
		return new SplitRow(label, NoTime, "", SplitPace.Unknown, "", SplitPace.Unknown);
	}

	private static string Gap(double time, double reference)
	{
		return reference <= 0d ? "" : TimeFormatter.FormatGap(time - reference);
	}

	/// <summary>A time: less of it is better.</summary>
	private static SplitPace Pace(double time, double reference)
	{
		if (reference <= 0d)
		{
			return SplitPace.Unknown;
		}

		return time <= reference ? SplitPace.Ahead : SplitPace.Behind;
	}

	/// <summary>A speed: more of it is better. The same shape as <see cref="Pace" /> and the opposite rule.</summary>
	private static SplitPace SpeedPace(double speed, double reference)
	{
		if (reference <= 0d)
		{
			return SplitPace.Unknown;
		}

		return speed >= reference ? SplitPace.Ahead : SplitPace.Behind;
	}

	private static string Speed(double kilometresPerHour)
	{
		return kilometresPerHour < 0d ? "" : UiNumbers.Text((int)kilometresPerHour);
	}
}
