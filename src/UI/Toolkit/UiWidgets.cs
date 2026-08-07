using System;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     The pieces every ATH panel is built from: a medal counter, a progress bar, a label/value
///     row, a button strip.
///     They live here rather than in the panels because the panels have to look like one mod
///     rather than three - once a counter is drawn in two places by two methods, it stops
///     being drawn the same way.
/// </summary>
public static class UiWidgets
{
	public static void MedalCount(ImGui gui, ImRect rect, Sprite sprite, int count, Color32 colour)
	{
		float iconSize = Mathf.Min(rect.H, rect.W * 0.5f);
		ImRect icon = rect.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect countRect);

		Color32 tint = count == 0 ? Color.Style.Text.Muted : colour;

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

	public static void Bar(ImGui gui, ImRect rect, float fraction, Color32 colour)
	{
		ImRectRadius radius = rect.H * 0.5f;

		gui.Canvas.Rect(rect, Color.Style.Surface.Track, radius);

		float filled = rect.W * Mathf.Clamp01(fraction);

		if (filled > 0f)
		{
			gui.Canvas.Rect(new ImRect(rect.X, rect.Y, filled, rect.H), colour, radius);
		}
	}

	public static void Row(ImGui gui, ImRect rect, string label, string value, Color32 valueColour)
	{
		ImRect labelRect = rect.TakeLeft(UiMetrics.LabelWidth(rect.W), out ImRect valueRect);

		UiText.Left(gui, label, Color.Style.Text.Muted, labelRect);
		UiText.Left(gui, value, valueColour, valueRect);
	}

	/// <summary>
	///     One choice on a fullscreen menu: what it does, and a line saying what that means, on a
	///     tile the whole of which is the button.
	///     A row of buttons stacked down a window is the right shape for a panel and the wrong one
	///     for a screen - a screen has room to say what each choice costs before it is taken.
	///     The tile used to lead with an icon, which is what a symbol is worth here: "Challenge
	///     History" and "Status" are both an i in a box, and neither of them is a shape anybody
	///     recognises before reading the words next to it. The words go first now, and the accent
	///     the icon carried is a rule under them.
	/// </summary>
	public static bool Card(ImGui gui, ImRect rect, string title, string caption, Color32 accent, bool enabled)
	{
		if (!enabled)
		{
			DrawCard(gui, rect, title, caption, Color.Style.Text.Muted, Color.Style.Surface.Track,
				Color.Style.Text.Muted);

			return false;
		}

		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, rect);
		bool hovered = gui.IsControlHovered(id);

		DrawCard(gui, rect, title, caption, accent,
			hovered ? Color.Style.Surface.TileHovered : Color.Style.Surface.Tile, Color.Style.Surface.White);

		return clicked;
	}

	/// <summary>
	///     Top down: title, rule, caption. The title's row is exactly the line height of the size
	///     it is drawn at, taken from the font rather than guessed at as a multiple of the size -
	///     a guess that came out one notch short is what left every tile on the menu titleless.
	/// </summary>
	private static void DrawCard(ImGui gui, ImRect rect, string title, string caption, Color32 accent,
		Color32 back, Color32 titleColour)
	{
		float text = gui.Style.Layout.TextSize;
		float titleSize = text * 1.2f;
		float pad = rect.H * 0.1f;
		float bar = Mathf.Max(2f, rect.H * 0.03f);
		float gap = gui.Style.Layout.InnerSpacing;

		gui.Canvas.Rect(rect, back, rect.H * 0.1f);

		ImRect inner = new(rect.X + pad, rect.Y + pad, rect.W - pad * 2f, rect.H - pad * 2f);
		ImRect head = inner.TakeTop(UiText.LineHeight(gui, titleSize), gap, out ImRect body);

		UiText.Draw(gui, title, titleColour, head, titleSize, 0f);

		gui.Canvas.Rect(new ImRect(body.X, body.Top - bar, body.W * 0.25f, bar), accent);
		UiText.Wrapped(gui, caption, Color.Style.Text.Muted,
			new ImRect(body.X, body.Y, body.W, Mathf.Max(0f, body.H - bar - gap)), text * 0.9f);
	}

	public static bool Clickable(ImGui gui, ImRect row)
	{
		uint id = gui.GetNextControlId();
		bool clicked = gui.InvisibleButton(id, row);

		if (gui.IsControlHovered(id))
		{
			gui.Canvas.Rect(row, Color.Style.Surface.Track, row.H * 0.2f);
		}

		return clicked;
	}

	public static ImRect Cell(ImRect row, ReadOnlySpan<float> weights, int column)
	{
		float offset = 0f;

		for (int i = 0; i < column; i++)
		{
			offset += weights[i];
		}

		return new ImRect(row.X + row.W * offset, row.Y, row.W * weights[column], row.H);
	}

	public static ImRect Column(ImGui gui, ImRect row, int index, int count)
	{
		float gap = gui.Style.Layout.InnerSpacing;
		float width = (row.W - gap * (count - 1)) / count;

		return new ImRect(row.X + index * (width + gap), row.Y, width, row.H);
	}

	public static void Heading(ImGui gui, ImRect rect, string text)
	{
		UiText.Draw(gui, text, Color.Style.Text.Section, rect, gui.Style.Layout.TextSize * 0.85f, 0f);
	}

	public static bool Button(ImGui gui, ImRect rect, string label)
	{
		return gui.Button(label.AsSpan(), rect);
	}

	public static bool IconButton(ImGui gui, ImRect rect, UiIcon icon, string label, Color32 accent)
	{
		return IconButton(gui, rect, icon, label, accent, true);
	}

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
		gui.Style.Button.Normal.FrontColor = Color.Style.Surface.White;
		gui.Style.Button.Hovered.FrontColor = Color.Style.Surface.White;
		gui.Style.Button.Pressed.FrontColor = Color.Style.Surface.White;

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
			Color.Style.Surface.White);

		return pressed;
	}

	private static void DrawDisabled(ImGui gui, ImRect rect, UiIcon icon, string label)
	{
		float inset = rect.H * 0.25f;

		gui.Canvas.Rect(rect, Color.Style.Surface.Track, rect.H * 0.2f);
		UiIcons.Draw(gui, new ImRect(rect.X + inset, rect.Y + inset, rect.H - inset * 2f, rect.H - inset * 2f), icon,
			Color.Style.Text.Muted);
		UiText.Centre(gui, label, Color.Style.Text.Muted, rect, gui.Style.Layout.TextSize);
	}

	private static Color32 Lighten(Color32 colour, float factor)
	{
		return new Color32((byte)Mathf.Clamp(colour.r * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.g * factor, 0f, 255f),
			(byte)Mathf.Clamp(colour.b * factor, 0f, 255f),
			colour.a);
	}

	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}
