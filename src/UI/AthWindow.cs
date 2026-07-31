using System;
using System.Collections.Generic;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;
using ZeepSDK.Chat;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The mod's window: the run HUD plus the controls that used to be chat commands.
///     Session-scoped, registered once for the whole game session rather than per run. It has
///     to outlive a run because it is also what a player sees when nothing is running - that
///     is where the Start button lives, and /ath is the way back to it.
/// </summary>
public class AthWindow : IZeepGUIDrawer
{
	private const string WindowTitle = "Author Time Hunting";

	/// <summary>Share of the screen width, before the clamp below.</summary>
	private const float WidthFraction = 0.22f;

	private const float MinWidth = 300f;
	private const float MaxWidth = 460f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton;

	private bool _mouseOverWindow;

	/// <summary>
	///     The run currently in progress, or null when ATH is idle. Set by StateMasterOn so
	///     the window always reflects reality rather than caching its own copy.
	///     Starting a run also shows the window: once a hunt is on, its state is the point.
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
		float height = UiMetrics.ClampHeight(gui, EstimateHeight(gui, view));

		ImRect rect = ImWindowPlacement.Place(gui, WindowTitle.AsSpan(), width, height, ImWindowAnchor.TopLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			// The height above is what the content wants; the screen, or the player's own
			// resize, may give it less. Scrolling is what makes the difference survivable.
			gui.BeginScrollable();

			try
			{
				if (view == null)
				{
					DrawIdle(gui, run != null);
				}
				else
				{
					DrawSection(gui, "Run Settings", view.Settings);
					DrawSection(gui, "Current Run", view.Run);
					DrawSection(gui, "Current Level", view.Level);
					DrawRunControls(gui, run);
				}
			}
			finally
			{
				gui.EndScrollable();
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
		Text(gui, starting ? "Waiting for the level to load..." : "No hunt running.", HudPalette.Muted, NextRow(gui));
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

	private static void DrawRunControls(ImGui gui, AthStateMachine run)
	{
		Text(gui, "Controls", HudPalette.Section, NextRow(gui));

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

		if (gui.Button((run.Ctx.IsPaused ? "Resume" : "Pause").AsSpan(), pauseRect))
		{
			if (run.Ctx.IsPaused)
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

	private static void DrawSection(ImGui gui, string title, IReadOnlyList<RunHudView.HudRow> rows)
	{
		if (rows == null || rows.Count == 0)
		{
			return;
		}

		Text(gui, title, HudPalette.Section, NextRow(gui));

		foreach (RunHudView.HudRow row in rows)
		{
			ImRect line = NextRow(gui);
			ImRect labelRect = line.TakeLeft(UiMetrics.LabelWidth(line.W), out ImRect valueRect);

			Text(gui, row.Label, HudPalette.Muted, labelRect);
			Text(gui, row.Value, row.ValueColour, valueRect);
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

	private static void Text(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		// Ellipsis, not overflow: a long level name must not paint over the next column.
		gui.Text(text.AsSpan(), colour, rect, false, ImTextOverflow.Ellipsis);
	}

	private static float EstimateHeight(ImGui gui, RunHudView view)
	{
		float spacing = gui.Style.Layout.Spacing;
		float buttonHeight = UiMetrics.ButtonHeight(gui);
		float chrome = UiMetrics.WindowChrome(gui);

		if (view == null)
		{
			// One line of status plus a single button.
			return gui.GetRowsHeightWithSpacing(1) + buttonHeight + 3 * spacing + chrome;
		}

		// Three sections with a heading each, then the "Controls" heading and three
		// button rows.
		const int sections = 3;
		int rows = sections + 1 + view.Settings.Count + view.Run.Count + view.Level.Count;

		return gui.GetRowsHeightWithSpacing(rows)
		       + 3 * (buttonHeight + spacing)
		       + sections * spacing
		       + chrome;
	}
}
