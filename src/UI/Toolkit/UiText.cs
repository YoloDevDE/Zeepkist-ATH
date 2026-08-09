using System;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Text drawn into a rect at an explicit size and alignment.
///     Imui's plain gui.Text draws at the theme's body size, left aligned. The HUD needs
///     neither: a clock is only a clock if it is large, and anything centred in a slice of
///     the screen has to be centred in the rect too, or it starts at an offset that moves
///     with the resolution.
///     Every rect here goes through <see cref="Fits" /> first, and that is not a nicety.
///     Anything but ImTextOverflow.Overflow makes Imui fit whole lines into the rect -
///     <c>(int)((boundsHeight + 0.001f) / LineHeight)</c> of them - and a rect one pixel short
///     of a single line gets zero lines and is drawn as nothing at all. No exception, no
///     warning, no text. A line is the font's line height rather than the size asked for, which
///     is the trap: a rect sized generously against the font size can still be under it. Sizing
///     the rect from the size is the mistake every caller makes once, so it is corrected here
///     instead of being written down for them.
/// </summary>
public static class UiText
{
	public static void Left(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		Draw(gui, text, colour, rect, gui.Style.Layout.TextSize, 0f);
	}

	public static void Centre(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		Draw(gui, text, colour, rect, size, 0.5f);
	}

	public static void Right(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		Draw(gui, text, colour, rect, size, 1f);
	}

	public static void Paragraph(ImGui gui, string text, Color32 colour)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		gui.Text(text.AsSpan(), colour, true);
	}

	/// <summary>A line of text that breaks into the rect rather than being cut off at its edge.</summary>
	public static void Wrapped(ImGui gui, string text, Color32 colour, ImRect rect, float size)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		ImTextSettings settings = new(size, 0f, 0f, true, ImTextOverflow.Ellipsis);
		gui.Text(text.AsSpan(), in settings, colour, Fits(gui, rect, size));
	}

	public static void Draw(ImGui gui, string text, Color32 colour, ImRect rect, float size, float alignX)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		ImTextSettings settings = new(size, alignX, 0.5f, false, ImTextOverflow.Ellipsis);
		gui.Text(text.AsSpan(), in settings, colour, Fits(gui, rect, size));
	}

	/// <summary>How tall a rect has to be before Imui will put a line of this size in it at all.</summary>
	public static float LineHeight(ImGui gui, float size)
	{
		return gui.TextDrawer.GetLineHeightFromFontSize(size);
	}

	/// <summary>
	///     How wide a line of text comes out. Only worth asking when several runs of text in
	///     different colours have to sit on one line: Imui draws one colour per call, so the line
	///     is laid out by measuring the pieces and handing each one the rect it ends in.
	/// </summary>
	public static float Width(ImGui gui, string text, float size)
	{
		if (string.IsNullOrEmpty(text))
		{
			return 0f;
		}

		ImTextSettings settings = new(size, 0f, 0.5f, false, ImTextOverflow.Overflow);

		return gui.MeasureTextSize(text.AsSpan(), in settings).x;
	}

	/// <summary>A rect too short for one line, grown around its own middle until it is not.</summary>
	private static ImRect Fits(ImGui gui, ImRect rect, float size)
	{
		float line = LineHeight(gui, size);

		if (rect.H >= line)
		{
			return rect;
		}

		return new ImRect(rect.X, rect.Y - (line - rect.H) * 0.5f, rect.W, line);
	}
}
