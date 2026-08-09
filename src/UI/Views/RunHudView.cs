using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the run HUD shows, as data. Built from the run once per draw and handed to whatever
///     renders it.
///     This is the seam the in-game UI is built on. The HUD used to exist only as a single
///     40-line string of TextMeshPro colour tags inside AthStateMachine, which meant the
///     layout, the numbers and the colours were one inseparable thing. Splitting them lets
///     the same run be rendered as an Imui panel without touching the run at all.
///     Three things read it - the bar across the top of the screen, the controls in its drawer,
///     and the level card in the corner - which is why it answers both how the run is going and
///     what level it is on. One frame's answer serves all three, and it is the same answer, so
///     they cannot drift apart mid-frame.
/// </summary>
public class RunHudView
{
	private static int _frame = -1;

	private static AthController _run;
	private static RunHudView _view;

	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	/// <summary>Whether the hour is actually being spent right now, which is what the live dot says.</summary>
	public bool Running { get; private set; }

	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	public static RunHudView ForFrame(AthController run)
	{
		if (_frame == Time.frameCount && ReferenceEquals(_run, run))
		{
			return _view;
		}

		_view = From(run);
		_frame = Time.frameCount;
		_run = run;

		return _view;
	}

	public static void Clear()
	{
		_frame = -1;
		_run = null;
		_view = null;
	}

	private static RunHudView From(AthController run)
	{
		AthCtx ctx = run?.Ctx;
		Level level = ctx?.CurrentLevel;

		if (level == null)
		{
			return null;
		}

		bool paused = ctx.IsPaused;
		double remaining = ctx.GetRemainingTime().TotalMilliseconds;

		bool running = !paused && level.IsTiming;

		Color32 timeColour = TimeLeftColour(ctx, running);

		return new RunHudView
		{
			Paused = paused,
			Running = running,
			TimeLeft = TimeFormatter.FormatDuration((int)remaining),
			TimeColour = timeColour,
			Timeline = BuildTimeline(ctx, timeColour),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			TimeLostToPenalties = TimeSpan.FromMilliseconds(ctx.GetAccumulatedPenaltyTime()).ToFormattedString(),
			SkipType = SkipTypeLabel(ctx),
			SkipColour = SkipTypeColour(ctx),
			LevelName = level.Name,
			Author = level.Author,
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			GoldTime = TimeFormatter.FormatTime(level.GoldTime)
		};
	}

	/// <summary>
	///     The hour so far, one segment per level, in the order they were played, with the
	///     penalties tacked on the end as the one stretch that was never driven.
	///     Broken levels get nothing, because their time is refunded: AthCtx leaves them out of
	///     the budget, and a bar that drew them would disagree with the clock beside it.
	/// </summary>
	private static IReadOnlyList<TimelineSegment> BuildTimeline(AthCtx ctx, Color32 liveColour)
	{
		if (ctx.Duration <= 0)
		{
			return [];
		}

		List<TimelineSegment> segments = [];

		foreach (Level level in ctx.Levels)
		{
			Add(segments, ctx.Duration, level.LevelBroken ? 0d : level.GetPlayDuration().TotalMilliseconds,
				SegmentColour(level, ctx.CurrentLevel, liveColour));
		}

		Add(segments, ctx.Duration, ctx.GetAccumulatedPenaltyTime(), Color.Style.Status.Danger);

		return segments;
	}

	private static void Add(List<TimelineSegment> segments, int duration, double milliseconds, Color32 colour)
	{
		if (milliseconds <= 0d)
		{
			return;
		}

		segments.Add(new TimelineSegment(Mathf.Clamp01((float)(milliseconds / duration)), colour));
	}

	/// <summary>
	///     What a level was worth, except for the one being driven - that one is still worth
	///     whatever the clock says, so it carries the clock's own colour and reddens with it.
	/// </summary>
	private static Color32 SegmentColour(Level level, Level current, Color32 liveColour)
	{
		if (ReferenceEquals(level, current))
		{
			return liveColour;
		}

		switch (level.Status)
		{
			case LevelStatus.Author:
				return Color.Zeepkist.Medal.Author;
			case LevelStatus.Gold:
				return Color.Zeepkist.Medal.Gold;
			case LevelStatus.Free:
				return Color.Style.Status.FreeSkip;
			case LevelStatus.Failed:
				return Color.Style.Status.Penalty;
			default:
				return Color.Style.Text.Muted;
		}
	}

	private static Color32 TimeLeftColour(AthCtx ctx, bool running)
	{
		if (!running)
		{
			return Color.Style.Text.Muted;
		}

		if (ctx.IsTimeRunningLow)
		{
			return Color.Style.Status.Danger;
		}

		return ctx.IsTimeAfterSkipRunningLow ? Color.Style.Status.Warning : Color.Style.Status.Good;
	}

	private static string SkipTypeLabel(AthCtx ctx)
	{
		if (ctx.CurrentLevel.AuthorTimeAcquired)
		{
			return "Author Skip";
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			return "Gold Skip";
		}

		if (ctx.AvaiableFreeSkips > 0)
		{
			return $"Free Skip ({ctx.AvaiableFreeSkips}x)";
		}

		return ctx.IsTimeRunningLow ? "FATAL SKIP" : "Penalty Skip";
	}

	private static Color32 SkipTypeColour(AthCtx ctx)
	{
		if (ctx.CurrentLevel.AuthorTimeAcquired)
		{
			return Color.Zeepkist.Medal.Author;
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			return Color.Zeepkist.Medal.Gold;
		}

		if (ctx.AvaiableFreeSkips > 0)
		{
			return Color.Style.Status.FreeSkip;
		}

		return ctx.IsTimeRunningLow ? Color.Style.Status.Fatal : Color.Style.Status.Penalty;
	}

	#region Time Budget

	public string TimeLeft { get; private set; }

	public Color32 TimeColour { get; private set; }

	public IReadOnlyList<TimelineSegment> Timeline { get; private set; } = [];

	#endregion

	#region Score

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	public string TimeLostToPenalties { get; private set; }

	#endregion

	#region Level

	public string LevelName { get; private set; }

	/// <summary>The name on its own. The bar writes the "by" itself, in a colour of its own.</summary>
	public string Author { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	#endregion
}
