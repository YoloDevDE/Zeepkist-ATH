using System;
using Imui.Controls;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The pieces every ATH panel is built from: a medal counter, a progress bar, a label/value
///     row, a button strip.
///     They live here rather than in the panels because the panels have to look like one mod
///     rather than three - once a counter is drawn in two places by two methods, it stops
///     being drawn the same way.
/// </summary>
internal static class UiWidgets
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

		if (sprite != null)
		{
			gui.Image(sprite, Square(icon), true);
		}
		else
		{
			gui.Canvas.Circle(icon.Center, iconSize * 0.3f, tint);
		}

		UiText.Draw(gui, count.ToString(), tint, countRect, gui.Style.Layout.TextSize * 1.5f, 0f);
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

	/// <summary>Centres a square inside a rect, so a non-square sprite is never stretched.</summary>
	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}