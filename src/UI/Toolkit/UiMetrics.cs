using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Screen-relative sizing for ATH's windows and overlays.
///     Imui's canvas already applies the game's UI scale, so a number here means the same
///     thing at any DPI. What the canvas does not tell us is how much room there actually is:
///     a fixed 340-unit window is a third of a 1024-wide canvas and a stripe on an ultrawide.
///     So sizes that should track the screen are a fraction of it with a clamp, and sizes that
///     should track the font (buttons, chrome) are derived from the row height instead.
///     The layout rows a panel is stacked from live here too, for the same reason: a row that is
///     one row height in one window and something else in the next is what makes two panels of
///     the same mod look like two mods.
/// </summary>
public static class UiMetrics
{
	public static float Margin(ImGui gui)
	{
		return gui.Style.Layout.Spacing * 2f;
	}

	public static float Width(ImGui gui, float fraction, float min, float max)
	{
		float available = gui.Canvas.SafeScreenRect.W - Margin(gui) * 2f;
		float low = Mathf.Min(min, available);
		float high = Mathf.Min(max, available);

		if (low > high)
		{
			low = high;
		}

		return Mathf.Clamp(gui.Canvas.SafeScreenRect.W * fraction, low, high);
	}

	public static float LabelWidth(float rowWidth)
	{
		return Mathf.Clamp(rowWidth * 0.45f, 80f, 240f);
	}

	public static float Slack(ImGui gui)
	{
		return gui.Style.Layout.Spacing;
	}

	public static float ContentHeight(ImGui gui)
	{
		return gui.Layout.GetFrame().Size.y;
	}

	public static float ButtonHeight(ImGui gui)
	{
		return gui.GetRowHeight() * 1.2f;
	}

	public static float ContentPadding(ImGui gui)
	{
		return gui.Style.Window.ContentPadding.Vertical;
	}

	public static float WindowChrome(ImGui gui)
	{
		return ContentPadding(gui) + ImWindow.GetTitleBarHeight(gui);
	}

	/// <summary>A full-width row that many row heights tall, with the layout spacing under it.</summary>
	public static ImRect Row(ImGui gui, float scale)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), gui.GetRowHeight() * scale);
	}

	/// <summary>The same, tall enough for a button.</summary>
	public static ImRect ButtonRow(ImGui gui)
	{
		return gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), ButtonHeight(gui));
	}

	/// <summary>
	///     What a window has to be to hold what was drawn in it last frame. Before there is a
	///     measurement, the caller's own guess in rows stands in - a window that opens at the wrong
	///     size for one frame is better than one that opens at zero.
	/// </summary>
	public static float WindowHeight(ImGui gui, float contentHeight, float fallbackRows)
	{
		float content = contentHeight > 0f ? contentHeight : gui.GetRowHeight() * fallbackRows;

		return content + WindowChrome(gui) + Slack(gui);
	}
}
