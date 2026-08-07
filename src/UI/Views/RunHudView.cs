using System;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     What the control panel shows about the run as a whole, as data. Built from
///     <see cref="AthCtx" /> once per draw and handed to whatever renders it.
///     This is the seam the in-game UI is built on. The HUD used to exist only as a single
///     40-line string of TextMeshPro colour tags inside AthStateMachine, which meant the
///     layout, the numbers and the colours were one inseparable thing. Splitting them lets
///     the same run be rendered as an Imui panel without touching the run at all.
///     Anything about the level being played is in <see cref="LevelStatsView" /> instead - the
///     split follows the two panels, and the two questions: how is the run going, and how is
///     this level going.
/// </summary>
public class RunHudView
{
	private static int _frame = -1;

	private static AthCtx _ctx;
	private static RunHudView _view;

	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	public static RunHudView ForFrame(AthCtx ctx)
	{
		if (_frame == Time.frameCount && ReferenceEquals(_ctx, ctx))
		{
			return _view;
		}

		_view = From(ctx);
		_frame = Time.frameCount;
		_ctx = ctx;

		return _view;
	}

	public static void Clear()
	{
		_frame = -1;
		_ctx = null;
		_view = null;
	}

	private static RunHudView From(AthCtx ctx)
	{
		if (ctx?.CurrentLevel == null)
		{
			return null;
		}

		bool paused = ctx.IsPaused;
		double remaining = ctx.GetRemainingTime().TotalMilliseconds;

		bool running = !paused && ctx.CurrentLevel.IsTiming;

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
			SkipColour = SkipTypeColour(ctx)
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
}
