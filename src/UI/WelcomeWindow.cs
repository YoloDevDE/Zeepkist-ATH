using System;
using System.Collections.Generic;
using AuthorTimeHunting.Gamemodes;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The screen a player meets before they ever press Start: what ATH is, what the gamemode
///     they are about to play actually asks of them, and the one button in the mod that can be
///     abused.
///     Everything here used to be knowledge you either had from somebody else or worked out by
///     losing a run to it - the clock, the skip penalties, the three minute ceiling on the level
///     pool. None of it is visible in the HUD, because the HUD shows what a run is doing, not
///     what its rules are.
///     Up when /ath opens the mod, and dismissable, rather than a page in a settings menu nobody
///     opens. That is also the only moment it appears on its own: a player already in a hunt is
///     not asking what the rules are. The checkbox at the bottom is the way out for good, and the
///     top bar is the way back in.
/// </summary>
public class WelcomeWindow : IZeepGUIDrawer
{
	private const string WindowTitle = "Welcome to ATH";

	private const float WidthFraction = 0.44f;
	private const float HeightFraction = 0.66f;
	private const float MinWidth = 480f;
	private const float MinHeight = 400f;

	private const float TitleSize = 2.1f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	/// <summary>The modes as this screen prints them, prepared when it opens. See <see cref="WelcomeMode" />.</summary>
	private WelcomeMode[] _modes = [];

	private bool _mouseOverWindow;

	/// <summary>
	///     Up or not. Set from the top bar, from /ath, and by the buttons here.
	///     Opening is also when the text is prepared: a window that draws itself sixty times a
	///     second should not be composing the same prose sixty times with it.
	/// </summary>
	public bool Visible
	{
		get;
		set
		{
			bool wasVisible = field;
			field = value;

			if (value && !wasVisible)
			{
				_modes = BuildModes();
			}
		}
	}

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		try
		{
			using (UiScale.Push(gui))
			{
				Draw(gui);
			}
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass - a throwing drawer would throw every frame.
			Logger.LogError($"WelcomeWindow: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui)
	{
		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = Mathf.Min(Mathf.Max(screen.W * WidthFraction, MinWidth), screen.W);
		float height = Mathf.Min(Mathf.Max(screen.H * HeightFraction, MinHeight), screen.H);

		// Centred and placed fresh every frame, like the run report: this is a moment in front
		// of everything else, not a panel that belongs in a corner.
		ImRect rect = new(screen.Left + (screen.W - width) * 0.5f,
			screen.Bottom + (screen.H - height) * 0.5f,
			width,
			height);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawHeadline(gui);
			DrawBody(gui);
			DrawFooter(gui);
		}
		finally
		{
			gui.EndWindow();
		}

		if (!open)
		{
			Visible = false;
		}
	}

	private static void DrawHeadline(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Centre(gui, "WELCOME TO", HudPalette.Muted, Row(gui, 1f), text * 0.9f);
		UiText.Centre(gui, "Author Time Hunting", HudPalette.White, Row(gui, TitleSize * 1.2f), text * TitleSize);
		UiText.Centre(gui, "Beat the author's time. Then do it again, until the hour is gone.", HudPalette.Author,
			Row(gui, 1.3f), text * 1.05f);

		gui.AddSpacing();
	}

	/// <summary>
	///     Everything between the headline and the buttons, scrolled. Drawn into a layout frame
	///     of its own so the scrollable stops above the footer - left in the window's own frame
	///     it would swallow the whole remainder and take the buttons with it.
	/// </summary>
	private void DrawBody(ImGui gui)
	{
		float footer = UiMetrics.ButtonHeight(gui) + gui.GetRowHeight() + gui.Style.Layout.Spacing * 3f;
		ImRect body = gui.AddLayoutRect(gui.GetLayoutWidth(),
			Mathf.Max(gui.GetLayoutHeight() - footer, gui.GetRowHeight()));

		gui.Layout.Push(ImAxis.Vertical, body);

		try
		{
			gui.BeginScrollable();

			try
			{
				DrawGamemodes(gui);
				DrawBrokenLevels(gui);
			}
			finally
			{
				gui.EndScrollable();
			}
		}
		finally
		{
			gui.Layout.Pop();
		}
	}

	private void DrawGamemodes(ImGui gui)
	{
		float text = gui.Style.Layout.TextSize;

		UiWidgets.Heading(gui, Row(gui, 1f), "THE GAMEMODE");

		foreach (WelcomeMode mode in _modes)
		{
			UiText.Draw(gui, mode.Name, HudPalette.Author, Row(gui, 1.3f), text * 1.15f, 0f);
			UiText.Paragraph(gui, mode.Description, HudPalette.Default);
			gui.AddSpacing();

			foreach (string rule in mode.Rules)
			{
				UiText.Paragraph(gui, rule, HudPalette.Default);
			}

			gui.AddSpacing();
		}

		UiText.Paragraph(gui,
			"More modes are on the way. Classic is the one that exists today, and it is the one every "
			+ "other mode will be measured against.", HudPalette.Muted);

		gui.AddSpacing();
	}

	/// <summary>
	///     The one warning worth putting on the first screen. Broken-skip refunds the time spent
	///     on the level, which makes it the only button in the mod that can undo a bad five
	///     minutes - and therefore the only one worth begging people not to reach for.
	/// </summary>
	private static void DrawBrokenLevels(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 1f), "WHEN A LEVEL IS BROKEN");

		UiText.Paragraph(gui,
			"The workshop has levels that simply cannot be finished - a checkpoint that never arms, a jump "
			+ "that does not exist any more, an author time nobody including the author has ever driven. "
			+ "'Level is Broken' throws that level out, refunds every second you spent on it, and draws "
			+ "another one.", HudPalette.Default);

		gui.AddSpacing();

		UiText.Paragraph(gui,
			"Which is exactly why it must not be used on a level that is merely hard. A run where the hard "
			+ "ones were declared broken is not a run - there is nothing left in it to be proud of, and "
			+ "nothing in it worth comparing to anybody else's. Hard levels get skipped, at the price the "
			+ "gamemode asks. Broken ones get reported.", HudPalette.Alert);

		gui.AddSpacing();
	}

	private void DrawFooter(ImGui gui)
	{
		bool show = Plugin.Instance.MyConfig.ShowWelcome.Value;

		if (gui.Checkbox(ref show, "Show this when ATH opens".AsSpan(), Row(gui, 1f)))
		{
			// Written straight through: BepInEx persists the file itself, and the switch has no
			// meaning until the next launch anyway.
			Plugin.Instance.MyConfig.ShowWelcome.Value = show;
		}

		ImRect row = gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), UiMetrics.ButtonHeight(gui));

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 0, 2), UiIcon.Info, "How do I use this?",
			    HudPalette.ActionRestart))
		{
			Plugin.Instance.Services.Help.Visible = true;
		}

		if (UiWidgets.IconButton(gui, UiWidgets.Column(gui, row, 1, 2), UiIcon.Play, "Let's hunt",
			    HudPalette.ActionResume))
		{
			Visible = false;
		}
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private static WelcomeMode[] BuildModes()
	{
		IReadOnlyList<IGamemode> modes = Plugin.Instance.Services.Gamemodes.All;
		WelcomeMode[] built = new WelcomeMode[modes.Count];

		for (int i = 0; i < modes.Count; i++)
		{
			built[i] = new WelcomeMode(modes[i]);
		}

		return built;
	}
}
