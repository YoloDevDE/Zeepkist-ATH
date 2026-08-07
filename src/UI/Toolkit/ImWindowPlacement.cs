using System;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

public enum ImWindowAnchor
{
	TopLeft,
	TopCenter,
	TopRight,
	MiddleLeft,
	MiddleRight,
	BottomLeft,
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
	public static ImRect PlaceAutoSized(ImGui gui, ReadOnlySpan<char> title, float width, float height,
		ImWindowAnchor anchor)
	{
		uint windowId = gui.GetControlId(title);
		ImRect screen = gui.Canvas.SafeScreenRect;
		float margin = UiMetrics.Margin(gui);

		if (gui.WindowManager.TryFindWindow(windowId) >= 0)
		{
			ref readonly ImRect existing = ref gui.WindowManager.GetWindowState(windowId).Rect;

			return Clamp(new ImRect(existing.X, existing.Y, width, height), screen, margin);
		}

		ImRect placed = anchor switch
		{
			ImWindowAnchor.TopLeft => new ImRect(screen.Left + margin, screen.Top - height - margin, width, height),
			ImWindowAnchor.TopCenter => new ImRect(screen.X + (screen.W - width) * 0.5f, screen.Top - height - margin,
				width, height),
			ImWindowAnchor.TopRight => new ImRect(screen.Right - width - margin, screen.Top - height - margin, width,
				height),
			ImWindowAnchor.BottomLeft => new ImRect(screen.Left + margin, screen.Bottom + margin, width, height),
			ImWindowAnchor.BottomRight => new ImRect(screen.Right - width - margin, screen.Bottom + margin, width,
				height),
			ImWindowAnchor.MiddleLeft => new ImRect(screen.Left + margin, screen.Bottom + (screen.H - height) * 0.5f,
				width, height),
			ImWindowAnchor.MiddleRight => new ImRect(screen.Right - width - margin,
				screen.Bottom + (screen.H - height) * 0.5f, width, height),
			_ => new ImRect(screen.Left + margin, screen.Bottom + margin, width, height)
		};

		return Clamp(placed, screen, margin);
	}

	private static ImRect Clamp(ImRect rect, ImRect screen, float margin)
	{
		rect.W = Mathf.Min(rect.W, screen.W);
		rect.H = Mathf.Min(rect.H, screen.H);

		rect.X = Mathf.Clamp(rect.X, screen.Left + margin - rect.W, screen.Right - margin);
		rect.Y = Mathf.Clamp(rect.Y, screen.Bottom, screen.Top - rect.H);

		return rect;
	}
}
