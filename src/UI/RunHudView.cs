using System;
using System.Collections.Generic;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     What the run HUD shows, as data. Built from <see cref="AthCtx" /> once per draw and
///     handed to whatever renders it.
///     This is the seam the in-game UI is built on. The HUD used to exist only as a single
///     40-line string of TextMeshPro colour tags inside AthStateMachine, which meant the
///     layout, the numbers and the colours were one inseparable thing. Splitting them lets
///     the same run be rendered as an Imui window without touching the run at all - and it
///     is why the old server message can be retired one renderer at a time.
///     The shape mirrors how the run is actually read: a handful of headline values the HUD
///     shows while driving, and <see cref="Details" /> for the control panel, which is opened
///     deliberately and can afford rows.
/// </summary>
public class RunHudView
{
	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	#region Headline

	/// <summary>Time left in the budget - the HUD's largest element by a wide margin.</summary>
	public string TimeLeft { get; private set; }

	public Color32 TimeColour { get; private set; }

	/// <summary>Share of the budget still unspent, 0..1, for the bar under the clock.</summary>
	public float RemainingFraction { get; private set; }

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	#endregion

	#region Current Level

	public string LevelTime { get; private set; }
	public string Attempt { get; private set; }

	/// <summary>What skipping right now would cost. The single most decision-relevant value.</summary>
	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	#endregion

	/// <summary>
	///     The rows too detailed for the HUD: the fixed run settings, and the penalty
	///     breakdown that shows what the budget would have been without them.
	/// </summary>
	public IReadOnlyList<HudRow> Details { get; private set; }

	/// <summary>
	///     Null when no level has been loaded yet, which happens between /ath start and the
	///     first level. Callers should skip drawing entirely in that case.
	/// </summary>
	public static RunHudView From(AthCtx ctx, bool paused)
	{
		if (ctx?.CurrentLevel == null)
		{
			return null;
		}

		double remaining = ctx.GetRemainingTime().TotalMilliseconds;

		return new RunHudView
		{
			Paused = paused,
			TimeLeft = TimeFormatter.FormatDuration((int)remaining),
			TimeColour = TimeLeftColour(ctx, paused),
			RemainingFraction = ctx.Duration <= 0 ? 0f : Mathf.Clamp01((float)(remaining / ctx.Duration)),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			LevelTime = TimeFormatter.FormatDuration((int)ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds),
			Attempt = ctx.CurrentLevel.Attempt.ToString(),
			SkipType = SkipTypeLabel(ctx),
			SkipColour = SkipTypeColour(ctx),
			Details = BuildDetails(ctx)
		};
	}

	private static HudRow[] BuildDetails(AthCtx ctx)
	{
		List<HudRow> rows =
		[
			new("Duration", TimeSpan.FromMilliseconds(ctx.Duration).ToFormattedString()),
			new("Skip Penalty", TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds).ToFormattedString(),
				HudPalette.Penalty),
			new("Free Skips Left", ctx.AvaiableFreeSkips.ToString(), HudPalette.FreeSkip)
		];

		if (ctx.Penalties > 0)
		{
			// What the budget would have been without penalties, so the cost of skipping
			// stays visible instead of silently vanishing into one number.
			rows.Add(new HudRow("Without Penalties",
				TimeFormatter.FormatDuration((int)ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds),
				HudPalette.Muted));
			rows.Add(new HudRow("Time Lost",
				TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds * ctx.Penalties).ToFormattedString(),
				HudPalette.Bad));
		}

		return rows.ToArray();
	}

	private static Color32 TimeLeftColour(AthCtx ctx, bool paused)
	{
		if (paused)
		{
			return HudPalette.Muted;
		}

		if (ctx.IsTimeRunningLow)
		{
			return HudPalette.Danger;
		}

		return ctx.IsTimeAfterSkipRunningLow ? HudPalette.Warning : HudPalette.Good;
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
			return HudPalette.Author;
		}

		if (ctx.CurrentLevel.GoldMedalAcquired)
		{
			return HudPalette.Gold;
		}

		if (ctx.AvaiableFreeSkips > 0)
		{
			return HudPalette.FreeSkip;
		}

		return ctx.IsTimeRunningLow ? HudPalette.Fatal : HudPalette.Penalty;
	}

	/// <summary>One label/value pair. The colour applies to the value, not the label.</summary>
	public readonly struct HudRow
	{
		public HudRow(string label, string value) : this(label, value, HudPalette.Default)
		{
		}

		public HudRow(string label, string value, Color32 valueColour)
		{
			Label = label;
			Value = value;
			ValueColour = valueColour;
		}

		public string Label { get; }
		public string Value { get; }
		public Color32 ValueColour { get; }
	}
}
