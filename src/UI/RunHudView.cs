using System;
using System.Collections.Generic;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.UI;

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
	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	#region Time Budget

	/// <summary>Time left in the budget - the panel's largest element by a wide margin.</summary>
	public string TimeLeft { get; private set; }

	public Color32 TimeColour { get; private set; }

	/// <summary>Share of the budget still unspent, 0..1, for the bar under the clock.</summary>
	public float RemainingFraction { get; private set; }

	/// <summary>The budget this run was given, fixed when it started.</summary>
	public string Duration { get; private set; }

	/// <summary>What one penalty skip costs.</summary>
	public string PenaltyTime { get; private set; }

	#endregion

	#region Score

	public int AuthorMedals { get; private set; }
	public int GoldMedals { get; private set; }
	public int Penalties { get; private set; }

	#endregion

	/// <summary>What skipping right now would cost. The most decision-relevant value here.</summary>
	public string SkipType { get; private set; }

	public Color32 SkipColour { get; private set; }

	/// <summary>
	///     The penalty breakdown, and empty until the first penalty is taken. Shown so the cost
	///     of skipping stays visible instead of silently vanishing into the one clock.
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
			Duration = TimeSpan.FromMilliseconds(ctx.Duration).ToFormattedString(),
			PenaltyTime = TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds).ToFormattedString(),
			AuthorMedals = ctx.AuthorMedals,
			GoldMedals = ctx.GoldMedals,
			Penalties = ctx.Penalties,
			SkipType = SkipTypeLabel(ctx),
			SkipColour = SkipTypeColour(ctx),
			Details = BuildDetails(ctx)
		};
	}

	private static HudRow[] BuildDetails(AthCtx ctx)
	{
		if (ctx.Penalties == 0)
		{
			return [];
		}

		return
		[
			new HudRow("Without Penalties",
				TimeFormatter.FormatDuration((int)ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds),
				HudPalette.Muted),
			new HudRow("Time Lost",
				TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds * ctx.Penalties).ToFormattedString(),
				HudPalette.Bad)
		];
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
