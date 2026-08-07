using System;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the run HUD and the control panel show, as data. Built from the run once per draw
///     and handed to whatever renders it.
///     This is the seam the in-game UI is built on. The HUD used to exist only as a single
///     40-line string of TextMeshPro colour tags inside AthStateMachine, which meant the
///     layout, the numbers and the colours were one inseparable thing. Splitting them lets
///     the same run be rendered as an Imui panel without touching the run at all.
///     It answers two questions, because one panel now asks both: how is the run going, and
///     what is the level it is on. The level half used to be its own view behind its own
///     window, and a window a player had to open to find out what they were driving was a
///     window they never opened.
/// </summary>
public class RunHudView
{
	private static int _frame = -1;

	private static AthStateMachine _run;
	private static RunHudView _view;

	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	public static RunHudView ForFrame(AthStateMachine run)
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

	private static RunHudView From(AthStateMachine run)
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

		return new RunHudView
		{
			Paused = paused,
			TimeLeft = TimeFormatter.FormatDuration((int)remaining),
			TimeColour = TimeLeftColour(ctx, running),
			RemainingFraction = ctx.Duration <= 0 ? 0f : Mathf.Clamp01((float)(remaining / ctx.Duration)),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			TimeLostToPenalties = TimeSpan.FromMilliseconds(ctx.GetAccumulatedPenaltyTime()).ToFormattedString(),
			SkipType = SkipTypeLabel(ctx),
			SkipColour = SkipTypeColour(ctx),
			LevelName = level.Name,
			ByAuthor = $"by {level.Author}",
			AuthorTime = TimeFormatter.FormatTime(level.AuthorTime),
			GoldTime = TimeFormatter.FormatTime(level.GoldTime),
			Attempts = run.IsBetweenAttempts ? $"{level.Attempt} (+1)" : UiNumbers.Text(level.Attempt)
		};
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

	public float RemainingFraction { get; private set; }

	#endregion

	#region Score

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	public string TimeLostToPenalties { get; private set; }

	#endregion

	#region Level

	public string LevelName { get; private set; }

	public string ByAuthor { get; private set; }

	public string AuthorTime { get; private set; }
	public string GoldTime { get; private set; }

	public string Attempts { get; private set; }

	#endregion
}
