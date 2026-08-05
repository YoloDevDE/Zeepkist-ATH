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
	/// <summary>Gap between a window and the screen edge.</summary>
	public static float Margin(ImGui gui)
	{
		return gui.Style.Layout.Spacing * 2f;
	}

	/// <summary>
	///     A fraction of the screen width, kept inside [<paramref name="min" />, <paramref name="max" />]
	///     and never wider than the screen itself - on a small canvas the screen wins over the minimum.
	/// </summary>
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

	/// <summary>
	///     The label column of a label/value row, as a share of the row it sits in. Fixed
	///     column widths either waste half a wide window or crush the value out of a narrow one.
	/// </summary>
	public static float LabelWidth(float rowWidth)
	{
		return Mathf.Clamp(rowWidth * 0.45f, 80f, 240f);
	}

	/// <summary>
	///     Head room added to every measured panel height. A panel that measures a hair short
	///     grows a scrollbar, and a scrollbar on a HUD is worse than a little empty space: it
	///     means the panel is now something you operate rather than read.
	/// </summary>
	public static float Slack(ImGui gui)
	{
		return gui.Style.Layout.Spacing;
	}

	/// <summary>
	///     How tall the content drawn into the current layout frame actually turned out.
	///     Must be read while the frame is still open - inside the window, before EndWindow.
	///     This is the honest answer to "how tall should this panel be". Adding up rows by hand
	///     is a second description of the layout that has to be kept in step with the first,
	///     and it never is: one forgotten spacing is a scrollbar. Letting the layout report
	///     itself costs a frame of lag on a size change and nothing else.
	/// </summary>
	public static float ContentHeight(ImGui gui)
	{
		return gui.Layout.GetFrame().Size.y;
	}

	/// <summary>
	///     Buttons follow the text size rather than the screen - a button scaled to a 4K canvas
	///     would be a slab.
	/// </summary>
	public static float ButtonHeight(ImGui gui)
	{
		return gui.GetRowHeight() * 1.2f;
	}

	/// <summary>
	///     Vertical space a window spends on itself. The title bar height used to be a guessed
	///     constant; Imui will tell us, and it moves with the theme.
	/// </summary>
	public static float WindowChrome(ImGui gui)
	{
		return gui.Style.Window.ContentPadding.Vertical + ImWindow.GetTitleBarHeight(gui);
	}
}
