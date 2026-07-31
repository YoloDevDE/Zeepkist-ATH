using System;
using Imui.Core;

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
	private const float Margin = 16f;

	public static ImRect GetRect(ImGui gui, ReadOnlySpan<char> title, float width, float height, ImWindowAnchor anchor)
	{
		uint windowId = gui.GetControlId(title);

		if (gui.WindowManager.TryFindWindow(windowId) >= 0)
		{
			// Already placed: keep the player's position, only take the new size.
			ref readonly ImRect existing = ref gui.WindowManager.GetWindowState(windowId).Rect;
			return new ImRect(existing.X, existing.Y, width, height);
		}

		ImRect screen = gui.Canvas.SafeScreenRect;

		return anchor switch
		{
			ImWindowAnchor.TopLeft => new ImRect(screen.Left + Margin, screen.Top - height - Margin, width, height),
			ImWindowAnchor.TopRight => new ImRect(screen.Right - width - Margin, screen.Top - height - Margin, width,
				height),
			ImWindowAnchor.BottomRight => new ImRect(screen.Right - width - Margin, screen.Bottom + Margin, width,
				height),
			ImWindowAnchor.MiddleLeft => new ImRect(screen.Left + Margin, screen.Bottom + (screen.H - height) * 0.5f,
				width, height),
			_ => new ImRect(screen.Left + Margin, screen.Bottom + Margin, width, height)
		};
	}
}
