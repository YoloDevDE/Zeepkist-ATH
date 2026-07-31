using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Draws the run HUD as an Imui window, the replacement for the server message block.
///     The renderer knows nothing about the run: it is handed a <see cref="RunHudView" />
///     through <see cref="Current" /> and draws whatever is in it. The run pushes a fresh
///     view; null means draw nothing.
/// </summary>
public class AthHudDrawer : IZeepGUIDrawer
{
	private const string WindowTitle = "Author Time Hunting";
	private const float WindowWidth = 340f;
	private const float LabelWidth = 140f;

	/// <summary>
	///     Imui reports no title bar height, so this is measured by eye. It only affects the
	///     window's initial size - the player can resize it.
	/// </summary>
	private const float TitleBarAllowance = 34f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton;

	private bool _mouseOverWindow;

	/// <summary>
	///     The snapshot to draw, or null to draw nothing. Written by the run, read here.
	/// </summary>
	public RunHudView Current { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		RunHudView view = Current;

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
			// This runs inside the game's GUI pass, shared with every other mod's drawer.
			// Stop drawing rather than keep throwing once per frame.
			Logger.LogError($"AthHudDrawer: Draw failed, hiding the HUD: {e.Message}\n{e.StackTrace}");
			Current = null;
		}
	}

	private void Draw(ImGui gui, RunHudView view)
	{
		ImRect rect = ImWindowPlacement.GetRect(gui, WindowTitle.AsSpan(), WindowWidth, EstimateHeight(gui, view),
			ImWindowAnchor.TopLeft);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawSection(gui, "Run Settings", view.Settings);
			DrawSection(gui, "Current Run", view.Run);
			DrawSection(gui, "Current Level", view.Level);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawSection(ImGui gui, string title, IReadOnlyList<RunHudView.HudRow> rows)
	{
		if (rows == null || rows.Count == 0)
		{
			return;
		}

		DrawText(gui, title, HudPalette.Section, NextRow(gui));

		foreach (RunHudView.HudRow row in rows)
		{
			ImRect line = NextRow(gui);
			ImRect labelRect = line.TakeLeft(LabelWidth, out ImRect valueRect);

			DrawText(gui, row.Label, HudPalette.Muted, labelRect);
			DrawText(gui, row.Value, row.ValueColour, valueRect);
		}

		gui.AddSpacing();
	}

	private static ImRect NextRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight());
	}

	/// <summary>
	///     Imui's own Text, not ZeepSDK's ImuiTextExtensions - the latter adds tooltips but is
	///     internal to the SDK and cannot be called from a mod.
	/// </summary>
	private static void DrawText(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		gui.Text(text.AsSpan(), colour, rect);
	}

	private static float EstimateHeight(ImGui gui, RunHudView view)
	{
		// Every section is a heading row plus its own rows, and ends with a blank line.
		const int sections = 3;
		int rows = sections + view.Settings.Count + view.Run.Count + view.Level.Count;

		return gui.GetRowsHeightWithSpacing(rows)
		       + sections * gui.Style.Layout.Spacing
		       + gui.Style.Window.ContentPadding.Vertical
		       + TitleBarAllowance;
	}
}
