using System;
using AuthorTimeHunting.Gamemodes;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     How to operate the mod: where its windows are, what mode is loaded, and what can be
///     typed into chat.
///     Kept apart from the welcome screen because the two answer different questions and are
///     wanted at different times. The welcome screen explains the game and is read once; this is
///     a reference and is opened again three runs later, when the question is which command
///     restarts a hunt. Putting the command list at the bottom of a page about gamemodes would
///     mean scrolling past the rules every time.
/// </summary>
public class HelpWindow : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Help";

	private const float WidthFraction = 0.26f;
	private const float MinWidth = 320f;
	private const float MaxWidth = 460f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoResizing;

	private float _contentHeight;
	private bool _mouseOverWindow;

	/// <summary>Up or not. Opened from the top bar and from the welcome screen.</summary>
	public bool Visible { get; set; }

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
			Logger.LogError($"HelpWindow: Draw failed, closing it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	public void Toggle()
	{
		Visible = !Visible;
	}

	private void Draw(ImGui gui)
	{
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		// Movable and remembered, unlike the welcome screen: this one is meant to be dragged
		// somewhere out of the way and left open while a hunt is set up.
		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.MiddleLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawMode(gui);
			DrawWindows(gui);
			DrawCommands(gui);

			// While the window's layout frame is still open, so it can report what it holds.
			_contentHeight = UiMetrics.ContentHeight(gui);
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

	/// <summary>
	///     Which mode the next Start will run. First, because it is the only line here that
	///     changes - the rest of the window is the same on every machine.
	/// </summary>
	private static void DrawMode(ImGui gui)
	{
		IGamemode mode = Plugin.Instance.Services.Gamemodes.Selected;

		UiWidgets.Heading(gui, Row(gui, 1f), "GAMEMODE");
		UiText.Left(gui, mode.DisplayName, HudPalette.Author, Row(gui, 1f));
		UiText.Paragraph(gui, mode.Description, HudPalette.Muted);
		gui.AddSpacing();
	}

	private static void DrawWindows(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 1f), "THE WINDOWS");

		UiText.Paragraph(gui,
			"Every window ATH draws is listed under 'ATH' in the game's top bar, with a tick beside the ones "
			+ "that are up. That is where they go away and that is where they come back - none of them can "
			+ "get lost.", HudPalette.Default);

		gui.AddSpacing();

		UiText.Paragraph(gui,
			"The controls only appear while the mouse cursor is on screen, because that is the only time "
			+ "they can be clicked. The skip buttons stay dead unless the lobby is actually racing.",
			HudPalette.Muted);

		gui.AddSpacing();
	}

	private static void DrawCommands(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 1f), "CHAT COMMANDS");

		Command(gui, "/ath start", "Starts a hunt. Add a mode to pick one, e.g. /ath start classic.");
		Command(gui, "/ath stop", "Ends the run and puts the report up.");
		Command(gui, "/ath restart", "Throws the run away and starts a fresh one.");
		Command(gui, "/ath broken", "Reports the level as unfinishable and refunds its time.");
		Command(gui, "/ath", "Shows or hides the mod's own windows.");
		Command(gui, "/athhistory", "Every run this machine has recorded.");
		Command(gui, "/athdebug", "The developer panel. Not for a real hunt.");
	}

	private static void Command(ImGui gui, string command, string what)
	{
		UiText.Left(gui, command, HudPalette.Command, Row(gui, 1f));
		UiText.Paragraph(gui, what, HudPalette.Muted);
		gui.AddSpacing();
	}

	private static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	private float Height(ImGui gui)
	{
		float content = _contentHeight > 0f ? _contentHeight : gui.GetRowHeight() * 20f;
		float available = gui.Canvas.SafeScreenRect.H - UiMetrics.Margin(gui) * 2f;

		return Mathf.Min(content + UiMetrics.WindowChrome(gui) + UiMetrics.Slack(gui), available);
	}
}
