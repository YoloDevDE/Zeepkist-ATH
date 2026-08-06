using System;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     How to operate the mod: where its windows are and what mode is loaded.
///     Kept apart from the welcome screen because the two answer different questions and are
///     wanted at different times. The welcome screen explains the game and is read once; this
///     is a reference and is opened again three runs later, when the question is where the
///     restart button went. Putting that at the bottom of a page about gamemodes would mean
///     scrolling past the rules every time.
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

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width, Height(gui),
			ImWindowAnchor.TopLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawMode(gui);
			DrawWindows(gui);
			DrawWayIn(gui);

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

	private static void DrawMode(ImGui gui)
	{
		IGamemode mode = Plugin.Instance.Services.Gamemodes.Selected;

		UiWidgets.Heading(gui, Row(gui, 1f), "GAMEMODE");
		UiText.Left(gui, mode.DisplayName, Color.Zeepkist.Medal.Author, Row(gui, 1f));
		UiText.Paragraph(gui, mode.Description, Color.Style.Text.Muted);
		gui.AddSpacing();
	}

	private static void DrawWindows(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 1f), "THE WINDOWS");

		UiText.Paragraph(gui,
			"Every window ATH draws can be switched on and off under Settings in the main menu. The run HUD "
			+ "sits across the top, the level and the controls in the corners, and none of them can get lost - "
			+ "the list is always there.", Color.Style.Text.Default);

		gui.AddSpacing();

		UiText.Paragraph(gui,
			"The skip buttons stay dead unless the lobby is actually racing.", Color.Style.Text.Muted);

		gui.AddSpacing();
	}

	private static void DrawWayIn(ImGui gui)
	{
		UiWidgets.Heading(gui, Row(gui, 1f), "THE WAY IN");

		UiText.Left(gui, "/ath", Color.Style.Text.Command, Row(gui, 1f));
		UiText.Paragraph(gui,
			"The mod's only chat command, and the same thing as clicking ATH in the game's top bar: it opens "
			+ "the main menu. Starting, stopping, restarting and the history all live in there.",
			Color.Style.Text.Muted);

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
