using System;
using System.Collections.Generic;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.Chat;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     ATH's control panel: the buttons that used to be chat commands, plus the run detail
///     that is too fine-grained for the HUD.
///     It no longer repeats the run state - <see cref="AthHud" /> owns that now and is always
///     on while a hunt runs. This window is opened on purpose with /ath and closed again, so
///     it can afford rows and a title bar.
///     Session-scoped, registered once for the whole game session: it is also what a player
///     sees when nothing is running, and that is where the Start button lives.
/// </summary>
public class AthWindow : IZeepGUIDrawer
{
	private const string WindowTitle = "Author Time Hunting";

	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.2f;

	private const float MinWidth = 280f;
	private const float MaxWidth = 400f;

	// No resizing: the height is recomputed from the content every frame, so a dragged
	// corner would spring back next frame anyway.
	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private bool _mouseOverWindow;

	/// <summary>
	///     The run currently in progress, or null when ATH is idle. Set by StateMasterOn so
	///     the window always reflects reality rather than caching its own copy.
	/// </summary>
	public AthStateMachine ActiveRun { get; set; }

	/// <summary>
	///     Toggled by /ath. Starts hidden and stays that way when a run begins - the HUD is
	///     what a hunt puts on screen; this is the panel you ask for.
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
			Logger.LogError($"AthWindow: Draw failed, hiding the window: {e.Message}\n{e.StackTrace}");
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
				DrawDetails(gui, view.Details);
				DrawRunControls(gui, run, view);
			}
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     Shown when no run is in progress. <paramref name="starting" /> covers the gap
	///     between /ath start and the first level, where a run exists but has no level yet.
	/// </summary>
	private static void DrawIdle(ImGui gui, bool starting)
	{
		UiText.Left(gui, starting ? "Waiting for the level to load..." : "No hunt running.", HudPalette.Muted,
			NextRow(gui));
		gui.AddSpacing();

		ImRect row = NextButtonRow(gui);

		if (starting)
		{
			if (gui.Button("Stop".AsSpan(), row))
			{
				CommandStop.Raise();
			}

			return;
		}

		if (gui.Button("Start Hunt".AsSpan(), row))
		{
			CommandStart.Raise();
		}
	}

	private static void DrawRunControls(ImGui gui, AthStateMachine run, RunHudView view)
	{
		UiText.Left(gui, "Controls", HudPalette.Section, NextRow(gui));

		ImRect skipRect = SplitRow(gui, NextButtonRow(gui), out ImRect brokenRect);

		if (gui.Button("Skip".AsSpan(), skipRect))
		{
			// The game's own skip, the same thing typing /fs does.
			ChatApi.SendMessage("/fs");
		}

		if (gui.Button("Broken Level".AsSpan(), brokenRect))
		{
			CommandSkipBroken.Raise();
		}

		ImRect pauseRect = SplitRow(gui, NextButtonRow(gui), out ImRect restartRect);

		if (gui.Button((view.Paused ? "Resume" : "Pause").AsSpan(), pauseRect))
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

		if (gui.Button("Restart".AsSpan(), restartRect))
		{
			CommandRestart.Raise();
		}

		if (gui.Button("Stop Hunt".AsSpan(), NextButtonRow(gui)))
		{
			CommandStop.Raise();
		}
	}

	private static void DrawDetails(ImGui gui, IReadOnlyList<RunHudView.HudRow> rows)
	{
		if (rows == null || rows.Count == 0)
		{
			return;
		}

		UiText.Left(gui, "Run", HudPalette.Section, NextRow(gui));

		foreach (RunHudView.HudRow row in rows)
		{
			ImRect line = NextRow(gui);
			ImRect labelRect = line.TakeLeft(UiMetrics.LabelWidth(line.W), out ImRect valueRect);

			UiText.Left(gui, row.Label, HudPalette.Muted, labelRect);
			UiText.Left(gui, row.Value, row.ValueColour, valueRect);
		}

		gui.AddSpacing();
	}

	/// <summary>Splits a row into two equal halves with the theme's own gap between them.</summary>
	private static ImRect SplitRow(ImGui gui, ImRect row, out ImRect right)
	{
		float gap = gui.Style.Layout.InnerSpacing;

		return row.TakeLeft((row.W - gap) * 0.5f, gap, out right);
	}

	private static ImRect NextRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight());
	}

	private static ImRect NextButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));
	}

	private static float MeasureHeight(ImGui gui, RunHudView view)
	{
		float spacing = gui.Style.Layout.Spacing;
		float buttonHeight = UiMetrics.ButtonHeight(gui);
		float chrome = UiMetrics.WindowChrome(gui);

		if (view == null)
		{
			// One line of status plus a single button.
			return gui.GetRowsHeightWithSpacing(1) + buttonHeight + 3 * spacing + chrome;
		}

		// The "Run" heading and its detail rows, then the "Controls" heading and three
		// button rows.
		int rows = 2 + view.Details.Count;

		return gui.GetRowsHeightWithSpacing(rows)
		       + 3 * (buttonHeight + spacing)
		       + spacing
		       + chrome;
	}
}
