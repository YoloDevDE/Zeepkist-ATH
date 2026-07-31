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
/// </summary>
public class RunHudView
{
	private RunHudView()
	{
	}

	public bool Paused { get; private set; }

	/// <summary>Fixed run settings, shown once at the top.</summary>
	public IReadOnlyList<HudRow> Settings { get; private set; }

	/// <summary>How the run as a whole is going.</summary>
	public IReadOnlyList<HudRow> Run { get; private set; }

	/// <summary>What is happening on the level right now.</summary>
	public IReadOnlyList<HudRow> Level { get; private set; }

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

		return new RunHudView
		{
			Paused = paused,
			Settings = new[]
			{
				new HudRow("Duration", TimeSpan.FromMilliseconds(ctx.Duration).ToFormattedString()),
				new HudRow("Skip Penalty", TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds).ToFormattedString(),
					HudPalette.Penalty)
			},
			Run = BuildRunRows(ctx, paused),
			Level = BuildLevelRows(ctx)
		};
	}

	private static HudRow[] BuildRunRows(AthCtx ctx, bool paused)
	{
		string timeLeft = TimeFormatter.FormatDuration((int)ctx.GetRemainingTime().TotalMilliseconds);

		if (ctx.Penalties > 0)
		{
			// Show what the budget would have been without penalties, so the cost of
			// skipping stays visible instead of silently vanishing into one number.
			string clean = TimeFormatter.FormatDuration((int)ctx.GetRemainingTimeWithoutPunishments().TotalMilliseconds);
			string lost = TimeSpan.FromMilliseconds(ctx.PenaltyTimeInMilliseconds * ctx.Penalties).ToFormattedString();
			timeLeft = $"{timeLeft}   ({clean} - {lost})";
		}

		return new[]
		{
			new HudRow("State", paused ? "PAUSED" : "ACTIVE", paused ? HudPalette.Muted : HudPalette.Good),
			new HudRow("Time Left", timeLeft, TimeLeftColour(ctx, paused)),
			new HudRow("AT / Gold / Skips", $"{ctx.AuthorMedals} / {ctx.GoldMedals} / {ctx.Penalties}")
		};
	}

	private static HudRow[] BuildLevelRows(AthCtx ctx)
	{
		return new[]
		{
			new HudRow("Level Time",
				TimeFormatter.FormatDuration((int)ctx.CurrentLevel.GetPlayDuration().TotalMilliseconds)),
			new HudRow("Skip Type", SkipTypeLabel(ctx), SkipTypeColour(ctx)),
			new HudRow("Attempt", ctx.CurrentLevel.Attempt.ToString())
		};
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
			return $"Free Skip ({ctx.AvaiableFreeSkips}x left)";
		}

		return ctx.IsTimeRunningLow ? "FATAL SKIP" : "Penalty Skip!";
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
