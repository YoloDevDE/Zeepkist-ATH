using System;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Draws whatever ATH last had to say as an in-game window: level summaries, medal
///     claims, the end-of-run report. Sits below the run HUD, which lives in its own window.
///     Only one panel is shown at a time - the newest replaces the previous one, the same way
///     a new chat message used to push the last one up.
/// </summary>
public class AthPanelDrawer : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH";
	private const float WindowWidth = 380f;
	private const float LabelWidth = 150f;
	private const float TitleBarAllowance = 34f;

	private const ImWindowFlag WindowFlags = ImWindowFlag.NoCloseButton;

	private bool _mouseOverWindow;
	private PanelView _panel;

	/// <summary>Realtime stamp when the panel was set, for the auto-hide timer.</summary>
	private float _shownAt;

	public void OnZeepGUI(ImGui gui)
	{
		PanelView panel = _panel;

		if (panel == null)
		{
			return;
		}

		if (panel.DisplaySeconds > 0f && Time.realtimeSinceStartup - _shownAt > panel.DisplaySeconds)
		{
			_panel = null;
			return;
		}

		try
		{
			Draw(gui, panel);
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass; a throwing drawer would throw every frame.
			Logger.LogError($"AthPanelDrawer: Draw failed, hiding the panel: {e.Message}\n{e.StackTrace}");
			_panel = null;
		}
	}

	public void Show(PanelView panel)
	{
		_panel = panel;
		_shownAt = Time.realtimeSinceStartup;
	}

	public void Clear()
	{
		_panel = null;
	}

	private void Draw(ImGui gui, PanelView panel)
	{
		ImRect rect = ImWindowPlacement.GetRect(gui, WindowTitle.AsSpan(), WindowWidth, EstimateHeight(gui, panel),
			ImWindowAnchor.BottomRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			if (!string.IsNullOrEmpty(panel.Title))
			{
				Text(gui, panel.Title, panel.TitleColour, NextRow(gui));
				gui.AddSpacing();
			}

			foreach (PanelBlock block in panel.Blocks)
			{
				DrawBlock(gui, block);
			}
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawBlock(ImGui gui, PanelBlock block)
	{
		switch (block.Kind)
		{
			case PanelBlockKind.Heading:
				gui.AddSpacing();
				Text(gui, block.Label, block.LabelColour, NextRow(gui));
				break;

			case PanelBlockKind.Line:
				Text(gui, block.Label, block.LabelColour, NextRow(gui));
				break;

			case PanelBlockKind.Row:
				ImRect line = NextRow(gui);
				ImRect labelRect = line.TakeLeft(LabelWidth, out ImRect valueRect);
				Text(gui, block.Label, block.LabelColour, labelRect);
				Text(gui, block.Value, block.ValueColour, valueRect);
				break;
		}
	}

	private static ImRect NextRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight());
	}

	private static void Text(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		gui.Text(text.AsSpan(), colour, rect);
	}

	private static float EstimateHeight(ImGui gui, PanelView panel)
	{
		int rows = panel.Blocks.Count + (string.IsNullOrEmpty(panel.Title) ? 0 : 1);
		int headings = 0;

		foreach (PanelBlock block in panel.Blocks)
		{
			if (block.Kind == PanelBlockKind.Heading)
			{
				headings++;
			}
		}

		return gui.GetRowsHeightWithSpacing(rows)
		       + (headings + 1) * gui.Style.Layout.Spacing
		       + gui.Style.Window.ContentPadding.Vertical
		       + TitleBarAllowance;
	}
}
