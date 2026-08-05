using System;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The pieces every ATH panel is built from: a medal counter, a progress bar, a label/value
///     row, a button strip.
///     They live here rather than in the panels because the panels have to look like one mod
///     rather than three - once a counter is drawn in two places by two methods, it stops
///     being drawn the same way.
/// </summary>
public static class UiWidgets
{
	/// <summary>
	///     A medal sprite with its count beside it. Falls back to a coloured dot when the game
	///     has no sprites loaded, which is the case in the main menu.
	/// </summary>
	public static void MedalCount(ImGui gui, ImRect rect, Sprite sprite, int count, Color32 colour)
	{
		float iconSize = Mathf.Min(rect.H, rect.W * 0.5f);
		ImRect icon = rect.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect countRect);

		// Zero is not news. Greying the whole counter sends the eye to the ones that moved.
		Color32 tint = count == 0 ? HudPalette.Muted : colour;

		DrawMedalIcon(gui, icon, sprite, iconSize, tint);

		UiText.Draw(gui, UiNumbers.Text(count), tint, countRect, gui.Style.Layout.TextSize * 1.5f, 0f);
	}

	private static void DrawMedalIcon(ImGui gui, ImRect icon, Sprite sprite, float iconSize, Color32 tint)
	{
		if (sprite == null)
		{
			gui.Canvas.Circle(icon.Center, iconSize * 0.3f, tint);

			return;
		}

		gui.Image(sprite, Square(icon), true);
	}

	/// <summary>
	///     A horizontal fill bar. A length is read without being parsed, which is the whole
	///     reason it sits next to a number that says the same thing.
	/// </summary>
	public static void Bar(ImGui gui, ImRect rect, float fraction, Color32 colour)
	{
		ImRectRadius radius = rect.H * 0.5f;

		gui.Canvas.Rect(rect, HudPalette.Track, radius);

		float filled = rect.W * Mathf.Clamp01(fraction);

		if (filled > 0f)
		{
			gui.Canvas.Rect(new ImRect(rect.X, rect.Y, filled, rect.H), colour, radius);
		}
	}

	/// <summary>A muted label on the left, the value on the right in its own colour.</summary>
	public static void Row(ImGui gui, ImRect rect, string label, string value, Color32 valueColour)
	{
		ImRect labelRect = rect.TakeLeft(UiMetrics.LabelWidth(rect.W), out ImRect valueRect);

		UiText.Left(gui, label, HudPalette.Muted, labelRect);
		UiText.Left(gui, value, valueColour, valueRect);
	}

	/// <summary>
	///     One column of a row split by weight rather than evenly, for the table-shaped lists.
	///     Every list in the mod had its own copy of this loop with its own weights declared
	///     inside it, which meant a fresh array per cell per row per frame - forty of them on a
	///     level list. The weights belong to the list and are declared once by the caller; this
	///     only does the arithmetic.
	/// </summary>
	public static ImRect Cell(ImRect row, ReadOnlySpan<float> weights, int column)
	{
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	/// <summary>Splits a row into equal columns with the theme's own gap between them.</summary>
	public static ImRect Column(ImGui gui, ImRect row, int index, int count)
	{
		float gap = gui.Style.Layout.InnerSpacing;
		float width = (row.W - gap * (count - 1)) / count;

		return new ImRect(row.X + index * (width + gap), row.Y, width, row.H);
	}

	/// <summary>
	///     Section heading. Deliberately not a separator line as well - a panel this small gets
	///     noisy fast, and the colour already does the separating.
	/// </summary>
	public static void Heading(ImGui gui, ImRect rect, string text)
	{
		UiText.Draw(gui, text, HudPalette.Section, rect, gui.Style.Layout.TextSize * 0.85f, 0f);
	}

	public static bool Button(ImGui gui, ImRect rect, string label)
	{
		return gui.Button(label.AsSpan(), rect);
	}

	/// <summary>
	///     A button in its own colour with its symbol beside the label.
	///     The symbol is drawn after the button rather than baked into its text: Imui centres a
	///     button's label and has no notion of an icon slot, so the only way to get both is to
	///     let it draw the button and then paint the shape into the gutter on the left. The
	///     colour goes through the theme for the same reason - a button paints its own
	///     background, so a rect drawn underneath would simply be covered.
	/// </summary>
	public static bool IconButton(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent)
	{
		return IconButton(gui, rect, icon, label, accent, true);
	}

	/// <summary>
	///     The same button, with an off switch. When <paramref name="enabled" /> is false it is
	///     not drawn as a button at all: Imui has no disabled state, and a control that still
	///     registers, still highlights on hover and then does nothing reads as broken rather than
	///     as unavailable. Nothing is registered, so nothing can be clicked, and the flat muted
	///     slab it leaves behind holds the layout so the strip does not reshuffle itself every
	///     time the lobby changes state.
	/// </summary>
	public static bool IconButton(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent, bool enabled)
	{
		if (!enabled)
		{
			DrawDisabled(gui, rect, icon, label);
			return false;
		}

		ImStyleButton previous = gui.Style.Button;

		gui.Style.Button.Normal.BackColor = accent;
		gui.Style.Button.Hovered.BackColor = Lighten(accent, 1.35f);
		gui.Style.Button.Pressed.BackColor = Lighten(accent, 0.75f);
		gui.Style.Button.Normal.FrontColor = HudPalette.White;
		gui.Style.Button.Hovered.FrontColor = HudPalette.White;
		gui.Style.Button.Pressed.FrontColor = HudPalette.White;

		bool pressed;

		try
		{
			pressed = gui.Button(label.AsSpan(), rect);
		}
		finally
		{
			gui.Style.Button = previous;
		}

		float inset = rect.H * 0.25f;
		UiIcons.Draw(gui, new ImRect(rect.X + inset, rect.Y + inset, rect.H - inset * 2f, rect.H - inset * 2f), icon,
			HudPalette.White);

		return pressed;
	}

	private static void DrawDisabled(ImGui gui, ImRect rect, UiIcon icon, string label)
	{
		float inset = rect.H * 0.25f;

		gui.Canvas.Rect(rect, HudPalette.Track, rect.H * 0.2f);
		UiIcons.Draw(gui, new ImRect(rect.X + inset, rect.Y + inset, rect.H - inset * 2f, rect.H - inset * 2f), icon,
			HudPalette.Muted);
		UiText.Centre(gui, label, HudPalette.Muted, rect, gui.Style.Layout.TextSize);
	}

	private static Color32 Lighten(Color32 colour, float factor)
	{
		return new Color32((byte)Mathf.Clamp(colour.r * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.g * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.b * factor, 0f, 255f),
			colour.a);
	}

	/// <summary>Centres a square inside a rect, so a non-square sprite is never stretched.</summary>
	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}
