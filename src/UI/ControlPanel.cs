using System;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
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
	private const float WidthFraction = 0.15f;

	private const float MinWidth = 210f;
	private const float MaxWidth = 300f;

	// Neither resizing nor moving: the height is recomputed from the content every frame, so
	// a dragged corner would spring back on the next one, and the panel's position is not its
	// own either - it hangs under the level panel, which is the one that can be dragged.
	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton | ImWindowFlag.NoMovingAndResizing;

	/// <summary>
	///     Height the content came to last frame, or 0 before the first one. See
	///     <see cref="UiMetrics.ContentHeight" /> for why this is measured rather than counted.
	/// </summary>
	private float _contentHeight;

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

	/// <summary>
	///     Whether the player can actually reach the buttons. Zeepkist hides the hardware cursor
	///     the moment you are driving, and a panel of buttons that cannot be clicked is just
	///     something in the way - so it goes away with the cursor and comes back with it.
	///     The cursor is the game's own signal, not a guess: it is what "Where is my Cursor?"
	///     surfaces too, and both agree because both read the same flag.
	/// </summary>
	private static bool CursorIsUsable => Cursor.visible;

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible || !CursorIsUsable)
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
		using (UiScale.Push(gui))
		{
			DrawScaled(gui);
		}
	}

	private void DrawScaled(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = run == null ? null : RunHudView.ForFrame(run.Ctx);

		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		// Under the level panel when there is one, in the same column and at the same width:
		// what the level is and what you can do about it is one thought, and it was split
		// across two corners of the screen. Auto-sized either way - the panel is as tall as
		// what it has to say and no taller.
		ImRect above = Plugin.Instance.Services.LevelStats.LastRect;

		ImRect rect = above.W > 0f ?
			ImWindowPlacement.Stack(gui, above, Height(gui)) :
			ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui), ImWindowAnchor.TopRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawBody(gui, run, view);

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
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
	private static void DrawBody(ImGui gui, AthStateMachine run, RunHudView view)
	{
		if (view == null)
		{
			DrawIdle(gui, run != null);

			return;
		}

		DrawControls(gui, run, view);
	}

	private static void TogglePause(AthStateMachine run, RunHudView view)
	{
		if (view.Paused)
		{
			run.ResumeRun();

			return;
		}

		run.PauseRun();
	}

	private static void DrawIdle(ImGui gui, bool starting)
	{
		UiText.Left(gui, starting ? "Waiting for the level to load..." : "No hunt running.", HudPalette.Muted,
			Row(gui, 1f));
		gui.AddSpacing();

		if (starting)
		{
			if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Stop, "Stop", HudPalette.ActionStop))
			{
				CommandStop.Raise();
			}

			return;
		}

		DrawGamemodePicker(gui);

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Play, "Start Hunt", HudPalette.ActionResume))
		{
			CommandStart.Raise();
		}
	}

	/// <summary>
	///     Which mode Start will run. A cycling button rather than a dropdown: there is no
	///     list control in Imui worth the trouble for a handful of entries, and the mode has
	///     to be readable at a glance anyway - a collapsed dropdown reads the same but costs
	///     a click to change. The button only appears once there is something to cycle to.
	/// </summary>
	private static void DrawGamemodePicker(ImGui gui)
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;

		UiWidgets.Heading(gui, Row(gui, 0.85f), "GAMEMODE");
		UiText.Left(gui, registry.Selected.DisplayName, HudPalette.Author, Row(gui, 1f));
		UiText.Draw(gui, registry.Selected.Description, HudPalette.Muted, Row(gui, 0.9f),
			gui.Style.Layout.TextSize * 0.85f, 0f);

		if (registry.All.Count < 2)
		{
			gui.AddSpacing();
			return;
		}

		if (UiWidgets.Button(gui, ButtonRow(gui), "Next Gamemode"))
		{
			registry.SelectNext();
		}

		gui.AddSpacing();
	}

	private static void DrawControls(ImGui gui, AthStateMachine run, RunHudView view)
	{
		DrawSkipSection(gui, view);
		DrawRunSection(gui, run, view);
	}

	/// <summary>
	///     Leaving the level, in its two forms. Kept apart from the run controls below because
	///     they answer different questions - this one is about the level in front of you, those
	///     are about the hour you are in - and because a Stop Hunt next to a Skip is a mistake
	///     waiting for a bad landing.
	/// </summary>
	private static void DrawSkipSection(ImGui gui, RunHudView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "SKIP");

		// Both of these skip the level out from under the lobby. Off the track - on the podium,
		// between levels, in a lobby that is not racing - the game has no level to skip and the
		// run's own bookkeeping has already moved on, so the skip lands on the wrong level or on
		// nothing at all. Not a warning afterwards: the button simply is not there to press.
		bool racing = GameStateObserver.IsRacing;

		// The button says what the skip would actually cost, in the colour of that cost: an
		// author skip is free and magenta, a penalty skip is five minutes and red, and the fatal
		// one that ends the run is near-black. A plain "Skip" made the cheapest and the most
		// expensive move in the mod look like the same button, which is exactly what it is not.
		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Skip, view.SkipType, view.SkipColour, racing))
		{
			// The game's own skip, the same thing typing /fs does.
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Warning, "Level is Broken", HudPalette.ActionBroken,
			    racing))
		{
			CommandSkipBroken.Raise();
		}

		gui.AddSpacing();
	}

	/// <summary>The hour itself: hold it, start it over, end it.</summary>
	private static void DrawRunSection(ImGui gui, AthStateMachine run, RunHudView view)
	{
		UiWidgets.Heading(gui, Row(gui, 0.85f), "RUN");

		ImRect row = ButtonRow(gui);

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), view.Paused ? UiIcon.Play : UiIcon.Pause,
			    view.Paused ? "Resume" : "Pause", view.Paused ? HudPalette.ActionResume : HudPalette.ActionPause))
		{
			TogglePause(run, view);
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.Restart, "Restart",
			    HudPalette.ActionRestart))
		{
			CommandRestart.Raise();
		}

		if (UiWidgets.IconButton(gui, ButtonRow(gui), UiIcon.Stop, "Stop Hunt", HudPalette.ActionStop))
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

	/// <summary>
	///     Last frame's content plus the window's own chrome. Before the first frame there is
	///     nothing to go on, so it opens generously - too tall is a moment of empty space, too
	///     short is a moment of scrollbar.
	/// </summary>
	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 8f;

		return content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui);
	}
}
