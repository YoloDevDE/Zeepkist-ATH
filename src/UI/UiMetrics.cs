using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Screen-relative sizing for ATH's windows and overlays.
///     Imui's canvas already applies the game's UI scale, so a number here means the same
///     thing at any DPI. What the canvas does not tell us is how much room there actually is:
///     a fixed 340-unit window is a third of a 1024-wide canvas and a stripe on an ultrawide.
///     So sizes that should track the screen are a fraction of it with a clamp, and sizes that
///     should track the font (buttons, chrome) are derived from the row height instead.
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
}
