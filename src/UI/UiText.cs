using System;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Text drawn into a rect at an explicit size and alignment.
///     Imui's plain gui.Text draws at the theme's body size, left aligned. The HUD needs
///     neither: a clock is only a clock if it is large, and anything centred in a slice of
///     the screen has to be centred in the rect too, or it starts at an offset that moves
///     with the resolution.
/// </summary>
internal static class UiText
{
	/// <summary>Left aligned, vertically centred, at the body text size.</summary>
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

	/// <summary>
	///     A block of prose that wraps and takes as many layout rows as it needs.
	///     The only text in the mod that is read rather than glanced at, so it is the only text
	///     that wraps: everything else lives in a fixed row and is cut with an ellipsis. Sized by
	///     the layout rather than by a caller-supplied rect, because how tall a paragraph comes
	///     out depends on the window width, and nobody can be asked to know that in advance.
	/// </summary>
	public static void Paragraph(ImGui gui, string text, Color32 colour)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		gui.Text(text.AsSpan(), colour, true, ImTextOverflow.Overflow);
	}

	public static void Draw(ImGui gui, string text, Color32 colour, ImRect rect, float size, float alignX)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		// Ellipsis rather than overflow: level and author names are arbitrary length and
		// must not paint over whatever sits beside them.
		ImTextSettings settings = new(size, alignX, 0.5f, false, ImTextOverflow.Ellipsis);
		gui.Text(text.AsSpan(), in settings, colour, rect);
	}
}
