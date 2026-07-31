using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The run HUD, in the shape Trackmania's Random Map Challenge overlays settled on: one
///     large clock, a bar for the budget behind it, and the score as big numbers under small
///     labels. Nothing here is interactive and nothing has a title bar - it is read at a
///     glance while driving, so it is drawn straight onto the canvas.
///     The previous HUD was a table of eight label/value rows in a draggable window. That is a
///     settings dialog, not a heads-up display: every value had equal weight, so the one
///     number that decides the run - how much time is left - had to be found rather than seen.
///     The detail moved to <see cref="AthWindow" />, which is opened on purpose and can afford
///     rows.
/// </summary>
public class AthHud : IZeepGUIDrawer
{
	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.3f;

	private const float MinWidth = 420f;
	private const float MaxWidth = 700f;

	/// <summary>Distance from the top of the screen, as a share of its height.</summary>
	private const float TopFraction = 0.03f;

	// Everything below is a multiple of the theme's body text size, so the HUD keeps its
	// proportions whatever the game's UI scale is set to.
	private const float ClockSize = 2.6f;
	private const float CounterSize = 1.7f;
	private const float LabelSize = 0.72f;
	private const float FooterSize = 0.85f;
	private const float PaddingSize = 0.8f;
	private const float BarSize = 0.32f;

	/// <summary>
	///     The run in progress, or null when ATH is idle. Set by StateMasterOn - the HUD is
	///     the thing that appears when a hunt starts, so it has no visibility toggle of its
	///     own: a run is on or it is not.
	/// </summary>
	public AthStateMachine ActiveRun { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = run == null ? null : RunHudView.From(run.Ctx, run.Ctx.IsPaused);

		if (view == null)
		{
			return;
		}

		try
		{
			Draw(gui, view);
		}
		catch (Exception e)
		{
			// Shared GUI pass: a drawer that throws would throw every frame.
			Logger.LogError($"AthHud: Draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	private static void Draw(ImGui gui, RunHudView view)
	{
		float text = gui.Style.Layout.TextSize;
		float gap = gui.Style.Layout.Spacing;
		float padding = text * PaddingSize;

		float clockHeight = text * ClockSize * 1.15f;
		float barHeight = Mathf.Max(text * BarSize, 3f);
		float counterHeight = text * CounterSize * 1.2f;
		float labelHeight = text * LabelSize * 1.6f;
		float footerHeight = text * FooterSize * 1.6f;

		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);
		float height = padding * 2f
		               + clockHeight
		               + gap + barHeight
		               + gap * 2f + counterHeight + labelHeight
		               + gap * 2f + footerHeight;

		ImRect screen = gui.Canvas.SafeScreenRect;
		ImRect panel = new(screen.Left + (screen.W - width) * 0.5f,
			screen.Top - screen.H * TopFraction - height,
			width,
			height);

		gui.Canvas.Rect(panel, HudPalette.Surface, new ImRectRadius(text * 0.5f));

		ImRect content = new(panel.X + padding, panel.Y + padding, panel.W - padding * 2f, panel.H - padding * 2f);

		ImRect clock = content.TakeTop(clockHeight, gap, out ImRect rest);
		UiText.Centre(gui, view.TimeLeft, view.TimeColour, clock, text * ClockSize);

		ImRect bar = rest.TakeTop(barHeight, gap * 2f, out rest);
		DrawBudgetBar(gui, bar, view);

		ImRect counters = rest.TakeTop(counterHeight, 0f, out rest);
		ImRect labels = rest.TakeTop(labelHeight, gap * 2f, out rest);
		DrawCounters(gui, counters, labels, view, text);

		DrawFooter(gui, rest.TakeTop(footerHeight), view, text);
	}

	/// <summary>
	///     How much of the budget is left, as a bar. The clock says the same thing, but a
	///     length is read without being parsed - that is the point of it being here.
	/// </summary>
	private static void DrawBudgetBar(ImGui gui, ImRect bar, RunHudView view)
	{
		ImRectRadius radius = bar.H * 0.5f;

		gui.Canvas.Rect(bar, HudPalette.Track, radius);

		float filled = bar.W * view.RemainingFraction;

		if (filled > 0f)
		{
			gui.Canvas.Rect(new ImRect(bar.X, bar.Y, filled, bar.H), view.TimeColour, radius);
		}
	}

	/// <summary>Three big numbers over three small labels, in the same three columns.</summary>
	private static void DrawCounters(ImGui gui, ImRect numbers, ImRect labels, RunHudView view, float text)
	{
		float column = numbers.W / 3f;

		DrawCounter(gui, numbers, labels, 0, column, view.AuthorMedals, "AUTHOR", HudPalette.Author, text);
		DrawCounter(gui, numbers, labels, 1, column, view.GoldMedals, "GOLD", HudPalette.Gold, text);
		DrawCounter(gui, numbers, labels, 2, column, view.Penalties, "SKIPS", HudPalette.Penalty, text);
	}

	private static void DrawCounter(ImGui gui, ImRect numbers, ImRect labels, int index, float column, int value,
		string label, Color32 colour, float text)
	{
		ImRect numberRect = new(numbers.X + index * column, numbers.Y, column, numbers.H);
		ImRect labelRect = new(labels.X + index * column, labels.Y, column, labels.H);

		// A count of zero is not news; grey it out so the eye goes to the ones that moved.
		Color32 numberColour = value == 0 ? HudPalette.Muted : colour;

		UiText.Centre(gui, value.ToString(), numberColour, numberRect, text * CounterSize);
		UiText.Centre(gui, label, HudPalette.Muted, labelRect, text * LabelSize);
	}

	/// <summary>
	///     The current level, split three ways so the skip type - the one thing here worth a
	///     colour - can keep its own and still sit at a predictable edge.
	/// </summary>
	private static void DrawFooter(ImGui gui, ImRect footer, RunHudView view, float text)
	{
		float size = text * FooterSize;
		float third = footer.W / 3f;

		ImRect left = footer.TakeLeft(third, out ImRect rest);
		ImRect centre = rest.TakeLeft(third, out ImRect right);

		UiText.Draw(gui, $"Level {view.LevelTime}", HudPalette.Muted, left, size, 0f);
		UiText.Centre(gui, $"Attempt {view.Attempt}", HudPalette.Muted, centre, size);

		if (view.Paused)
		{
			UiText.Right(gui, "PAUSED", HudPalette.Warning, right, size);
			return;
		}

		UiText.Right(gui, view.SkipType, view.SkipColour, right, size);
	}
}
