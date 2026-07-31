using System;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.Chat;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     ATH's main panel, in the shape Trackmania's Random Map Challenge settled on: the clock
///     first and large, the budget behind it as a bar, the score as the game's own medals with
///     a count beside each, and the controls underneath.
///     It replaces the two panels that came before - a chrome-less strip of numbers and a
///     separate window of buttons. Splitting them meant the thing you look at and the thing
///     you act on were never in the same place, and neither was the whole mod. This is.
///     Session-scoped, registered once for the whole game session: it is also what a player
///     sees when nothing is running, and that is where the Start button lives.
/// </summary>
public class ControlPanel : IZeepGUIDrawer
{
	private const string WindowTitle = "Author Time Hunting";

	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.21f;

	private const float MinWidth = 320f;
	private const float MaxWidth = 440f;

	// Multiples of the theme's body text size, so the panel keeps its proportions whatever
	// the game's UI scale is set to.
	private const float ClockSize = 2.4f;
	private const float MedalRowSize = 1.8f;
	private const float BarSize = 0.3f;
	private const float FooterSize = 0.85f;

	// No resizing: the height is recomputed from the content every frame, so a dragged
	// corner would spring back on the next one.
	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private bool _mouseOverWindow;

	/// <summary>
	///     The run currently in progress, or null when ATH is idle. Set by StateMasterOn so
	///     the panel always reflects reality rather than caching its own copy.
	///     Starting a run also shows the panel: once a hunt is on, its state is the point.
	/// </summary>
	public AthStateMachine ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value != null)
			{
				Visible = true;
			}
		}
	}

	/// <summary>
	///     Toggled by /ath. Starts hidden so the mod stays out of the way until asked for,
	///     and is switched on automatically when a run begins.
	/// </summary>
	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
			Logger.LogError($"ControlPanel: Draw failed, hiding the panel: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = run == null ? null : RunHudView.From(run.Ctx, run.Ctx.IsPaused);

		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		// Auto-sized: the panel is as tall as what it has to say and no taller.
		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, MeasureHeight(gui, view),
			ImWindowAnchor.TopLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			if (view == null)
			{
				DrawIdle(gui, run != null);
			}
			else
			{
				DrawRun(gui, view);
				DrawControls(gui, run, view);
			}
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawRun(ImGui gui, RunHudView view)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, view.TimeLeft, view.TimeColour, Row(gui, ClockSize * 1.15f), text * ClockSize);
		UiWidgets.Bar(gui, Row(gui, BarSize), view.RemainingFraction, view.TimeColour);

		DrawBudgetFooter(gui, view, text);
		gui.AddSpacing();

		DrawMedals(gui, view);

		foreach (RunHudView.HudRow row in view.Details) UiWidgets.Row(gui, Row(gui, 1f), row.Label, row.Value, row.ValueColour);

		gui.AddSpacing();
	}

	/// <summary>
	///     What the run was given and what a mistake costs, on one muted line. Both are fixed
	///     for the whole run, so they are context rather than news - hence the small type.
	/// </summary>
	private static void DrawBudgetFooter(ImGui gui, RunHudView view, float text)
	{
		ImRect footer = Row(gui, FooterSize * 1.4f);
		float size = text * FooterSize;

		ImRect left = footer.TakeLeft(footer.W * 0.5f, out ImRect right);

		UiText.Draw(gui, $"of {view.Duration}", HudPalette.Muted, left, size, 0f);

		if (view.Paused)
		{
			UiText.Right(gui, "PAUSED", HudPalette.Warning, right, size);
			return;
		}

		UiText.Right(gui, view.SkipType, view.SkipColour, right, size);
	}

	/// <summary>
	///     The score, as the game's own medals. Sprites rather than words because this is the
	///     line a player checks mid-run, and three shapes are read faster than three labels.
	/// </summary>
	private static void DrawMedals(ImGui gui, RunHudView view)
	{
		ImRect row = Row(gui, MedalRowSize);

		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 0, 3), GameSprites.AuthorMedal, view.AuthorMedals,
			HudPalette.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 1, 3), GameSprites.GoldMedal, view.GoldMedals,
			HudPalette.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, row, 2, 3), GameSprites.YouTriedMedal, view.Penalties,
			HudPalette.Penalty);
	}

	/// <summary>
	///     Shown when no run is in progress. <paramref name="starting" /> covers the gap
	///     between /ath start and the first level, where a run exists but has no level yet.
	/// </summary>
	private static void DrawIdle(ImGui gui, bool starting)
	{
		UiText.Left(gui, starting ? "Waiting for the level to load..." : "No hunt running.", HudPalette.Muted,
			Row(gui, 1f));
		gui.AddSpacing();

		ImRect row = ButtonRow(gui);

		if (starting)
		{
			if (UiWidgets.Button(gui, row, "Stop"))
			{
				CommandStop.Raise();
			}

			return;
		}

		if (UiWidgets.Button(gui, row, "Start Hunt"))
		{
			CommandStart.Raise();
		}
	}

	private static void DrawControls(ImGui gui, AthStateMachine run, RunHudView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "CONTROLS");

		ImRect first = ButtonRow(gui);

		if (UiWidgets.Button(gui, UiWidgets.Column(gui, first, 0, 2), "Skip"))
		{
			// The game's own skip, the same thing typing /fs does.
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.Button(gui, UiWidgets.Column(gui, first, 1, 2), "Broken Level"))
		{
			CommandSkipBroken.Raise();
		}

		ImRect second = ButtonRow(gui);

		if (UiWidgets.Button(gui, UiWidgets.Column(gui, second, 0, 2), view.Paused ? "Resume" : "Pause"))
		{
			if (view.Paused)
			{
				run.ResumeRun();
			}
			else
			{
				run.PauseRun();
			}
		}

		if (UiWidgets.Button(gui, UiWidgets.Column(gui, second, 1, 2), "Restart"))
		{
			CommandRestart.Raise();
		}

		if (UiWidgets.Button(gui, ButtonRow(gui), "Stop Hunt"))
		{
			CommandStop.Raise();
		}
	}

	/// <summary>A layout row <paramref name="scale" /> times the theme's text size tall.</summary>
	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
	}

	private static float MeasureHeight(ImGui gui, RunHudView view)
	{
		float row = gui.GetRowHeight();
		float spacing = gui.Style.Layout.Spacing;
		float buttonHeight = UiMetrics.ButtonHeight(gui);
		float chrome = UiMetrics.WindowChrome(gui);

		if (view == null)
		{
			// One line of status plus a single button.
			return row + buttonHeight + 4 * spacing + chrome;
		}

		// Clock, bar, budget footer, medals, the controls heading, then three button rows.
		float content = row * (ClockSize * 1.15f + BarSize + FooterSize * 1.4f + MedalRowSize + 0.85f)
		                + row * view.Details.Count
		                + 3 * buttonHeight;

		int gaps = 7 + view.Details.Count + 2;

		return content + gaps * spacing + chrome;
	}
}
