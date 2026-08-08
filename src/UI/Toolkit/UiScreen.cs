using AuthorTimeHunting.Util;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     What a screen that takes the whole game over is made of: how wide its content is allowed
///     to be, the rule under its title, and the order it has to be drawn at.
///     Two of the mod's screens are looked at while nothing else is happening - the menu before a
///     hunt and the card between two levels - and both are the only thing worth reading when they
///     are up. A panel sized to stay out of the way is the wrong shape for that, so they get the
///     whole screen, and this is what keeps the two of them looking like the same mod.
///     The content is a column in the middle rather than the full width, because a button that
///     runs the length of an ultrawide is not a button anyone aims at.
/// </summary>
public static class UiScreen
{
	/// <summary>
	///     Above every Imui window, below its popups. Imui hands each window a slice of 128
	///     starting at zero and starts popups at 1048576, so a screen drawn at this order covers
	///     the mod's own panels - which is what fullscreen has to mean - without swallowing a
	///     dropdown opened on top of it.
	/// </summary>
	public const int Order = 1 << 16;

	private const float _columnFraction = 0.62f;
	private const float _minColumn = 520f;
	private const float _maxColumn = 1100f;

	private const float _verticalMargin = 0.06f;

	public static ImRect Full(ImGui gui)
	{
		return gui.Canvas.SafeScreenRect;
	}

	public static float Width(ImRect screen)
	{
		float low = Mathf.Min(_minColumn, screen.W);
		float high = Mathf.Min(_maxColumn, screen.W);

		return Mathf.Clamp(screen.W * _columnFraction, low, Mathf.Max(low, high));
	}

	public static ImRect Column(ImRect screen)
	{
		float width = Width(screen);
		float margin = screen.H * _verticalMargin;

		return new ImRect(screen.X + (screen.W - width) * 0.5f, screen.Y + margin, width, screen.H - margin * 2f);
	}

	/// <summary>A hairline the width of the content, with a lit stretch in the middle.</summary>
	public static void Rule(ImGui gui, ImRect rect, Color32 accent)
	{
		float thickness = Mathf.Max(1f, rect.H * 0.12f);
		float lit = rect.W * 0.34f;
		float y = rect.Y + (rect.H - thickness) * 0.5f;

		gui.Canvas.Rect(new ImRect(rect.X, y, rect.W, thickness), Color.Style.Surface.Track);
		gui.Canvas.Rect(new ImRect(rect.X + (rect.W - lit) * 0.5f, y, lit, thickness), accent);
	}

	/// <summary>A space between every letter, which is the only letter spacing Imui offers.</summary>
	public static string Spaced(string text)
	{
		return string.Join(" ", text.ToCharArray());
	}
}
