using System;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI;

public enum ImWindowAnchor
{
	TopLeft,
	TopRight,
	MiddleLeft,
	BottomRight
}

/// <summary>
///     Places an Imui window the first time it appears and then gets out of the way, so a
///     window the player dragged somewhere stays where they put it.
///     Same approach as GTR's helper of the same name - Imui has no notion of a default
///     anchor, it only remembers where a window already is.
/// </summary>
public static class ImWindowPlacement
{
	/// <summary>
	///     For a window the player owns. After the first frame both the position and the size
	///     are theirs to change: passing a computed size every frame would silently undo every
	///     drag of the resize handle, which is why the handle appeared to do nothing.
	/// </summary>
	public static ImRect Place(ImGui gui, ReadOnlySpan<char> title, float width, float height, ImWindowAnchor anchor)
	{
		return Place(gui, title, width, height, anchor, true);
	}

	/// <summary>
	///     For a window ATH sizes itself, such as a panel whose height follows its content.
	///     Keeps the player's position but always applies the given size.
	/// </summary>
	public static ImRect PlaceAutoSized(ImGui gui, ReadOnlySpan<char> title, float width, float height,
		ImWindowAnchor anchor)
	{
		return Place(gui, title, width, height, anchor, false);
	}

	private static ImRect Place(ImGui gui, ReadOnlySpan<char> title, float width, float height, ImWindowAnchor anchor,
		bool keepUserSize)
	{
		uint windowId = gui.GetControlId(title);
		ImRect screen = gui.Canvas.SafeScreenRect;
		float margin = UiMetrics.Margin(gui);

		if (gui.WindowManager.TryFindWindow(windowId) >= 0)
		{
			ref readonly ImRect existing = ref gui.WindowManager.GetWindowState(windowId).Rect;

			return Clamp(new ImRect(existing.X,
				existing.Y,
				keepUserSize ? existing.W : width,
				keepUserSize ? existing.H : height), screen, margin);
		}

		ImRect placed = anchor switch
		{
			ImWindowAnchor.TopLeft => new ImRect(screen.Left + margin, screen.Top - height - margin, width, height),
			ImWindowAnchor.TopRight => new ImRect(screen.Right - width - margin, screen.Top - height - margin, width,
				height),
			ImWindowAnchor.BottomRight => new ImRect(screen.Right - width - margin, screen.Bottom + margin, width,
				height),
			ImWindowAnchor.MiddleLeft => new ImRect(screen.Left + margin, screen.Bottom + (screen.H - height) * 0.5f,
				width, height),
			_ => new ImRect(screen.Left + margin, screen.Bottom + margin, width, height)
		};

		return Clamp(placed, screen, margin);
	}

	/// <summary>
	///     Pulls a window back onto the screen. Without this a window placed on a large display
	///     - or dragged near an edge - stays off-screen after a resolution change, with no way
	///     to reach its title bar and drag it back.
	/// </summary>
	private static ImRect Clamp(ImRect rect, ImRect screen, float margin)
	{
		rect.W = Mathf.Min(rect.W, screen.W);
		rect.H = Mathf.Min(rect.H, screen.H);

		// Keep at least a margin's worth of the window - and with it the title bar - reachable.
		rect.X = Mathf.Clamp(rect.X, screen.Left + margin - rect.W, screen.Right - margin);
		rect.Y = Mathf.Clamp(rect.Y, screen.Bottom, screen.Top - rect.H);

		return rect;
	}
}
