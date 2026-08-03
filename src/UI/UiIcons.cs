using System;
using Imui.Core;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>What a control does, as a transport-bar shape.</summary>
internal enum UiIcon
{
	None,
	Play,
	Pause,
	Stop,
	Skip,
	Restart,
	Warning,
	Info
}

/// <summary>
///     The transport symbols, drawn as triangles and bars rather than typed as characters.
///     A glyph would be one line of code, but only if the font has it: Imui ships its own
///     font atlas, and a missing U+23ED is a tofu box on the player's screen with nothing in
///     the log to explain it. Three points and a rect always render.
/// </summary>
internal static class UiIcons
{
	public static void Draw(ImGui gui, ImRect rect, UiIcon icon, Color32 colour)
	{
		if (icon == UiIcon.None)
		{
			return;
		}

		ImRect box = Square(rect);

		switch (icon)
		{
			case UiIcon.Play:
				Triangle(gui, box, colour, true);
				break;
			case UiIcon.Pause:
				Bars(gui, box, colour);
				break;
			case UiIcon.Stop:
				Stop(gui, box, colour);
				break;
			case UiIcon.Skip:
				Transport(gui, box, colour, true);
				break;
			case UiIcon.Restart:
				Transport(gui, box, colour, false);
				break;
			case UiIcon.Warning:
				Warning(gui, box, colour);
				break;
			case UiIcon.Info:
				Info(gui, box, colour);
				break;
		}
	}

	/// <summary>A filled triangle spanning the box, pointing left or right.</summary>
	private static void Triangle(ImGui gui, ImRect box, Color32 colour, bool right)
	{
		float tip = right ? box.Right : box.Left;
		float back = right ? box.Left : box.Right;

		Span<Vector2> points =
		[
			new(back, box.Bottom), new(back, box.Top), new(tip, box.Y + box.H * 0.5f)
		];

		gui.Canvas.ConvexFill(points, colour);
	}

	/// <summary>The pause pair: two bars with a gap of the same width between them.</summary>
	private static void Bars(ImGui gui, ImRect box, Color32 colour)
	{
		float bar = box.W * 0.3f;

		gui.Canvas.Rect(new ImRect(box.X, box.Y, bar, box.H), colour);
		gui.Canvas.Rect(new ImRect(box.Right - bar, box.Y, bar, box.H), colour);
	}

	private static void Stop(ImGui gui, ImRect box, Color32 colour)
	{
		float inset = box.W * 0.1f;

		gui.Canvas.Rect(new ImRect(box.X + inset, box.Y + inset, box.W - inset * 2f, box.H - inset * 2f), colour);
	}

	/// <summary>
	///     Skip and restart are the same shape mirrored - two triangles running into a bar. The
	///     bar is what makes it "to the end of this" rather than "fast forward", which is exactly
	///     what both buttons do: skip goes to the next level, restart goes back to the first.
	/// </summary>
	private static void Transport(ImGui gui, ImRect box, Color32 colour, bool forward)
	{
		float bar = box.W * 0.16f;
		float wedge = (box.W - bar) * 0.5f;

		float first = forward ? box.X : box.X + bar;
		float second = first + wedge;

		Triangle(gui, new ImRect(first, box.Y, wedge, box.H), colour, forward);
		Triangle(gui, new ImRect(second, box.Y, wedge, box.H), colour, forward);
		gui.Canvas.Rect(new ImRect(forward ? box.Right - bar : box.X, box.Y, bar, box.H), colour);
	}

	/// <summary>The broken-level marker: a triangle standing on its base, point up.</summary>
	private static void Warning(ImGui gui, ImRect box, Color32 colour)
	{
		Span<Vector2> points =
		[
			new(box.X, box.Bottom), new(box.X + box.W * 0.5f, box.Top), new(box.Right, box.Bottom)
		];

		gui.Canvas.ConvexFill(points, colour);
	}

	/// <summary>
	///     A lower-case i: the dot and the stem, nothing around them. Drawn rather than ringed
	///     because the ring would have to be knocked out of the middle to leave the stem visible,
	///     and the canvas can fill shapes but not subtract them - a solid disc with a stem the
	///     same colour is just a disc.
	/// </summary>
	private static void Info(ImGui gui, ImRect box, Color32 colour)
	{
		float stem = box.W * 0.24f;
		float dot = box.H * 0.2f;
		float gap = box.H * 0.12f;
		float x = box.X + (box.W - stem) * 0.5f;

		gui.Canvas.Rect(new ImRect(x, box.Top - dot, stem, dot), colour);
		gui.Canvas.Rect(new ImRect(x, box.Y, stem, box.H - dot - gap), colour);
	}

	/// <summary>The largest square that fits, centred, so nothing is ever stretched.</summary>
	private static ImRect Square(ImRect rect)
	{
		float size = Mathf.Min(rect.W, rect.H);

		return new ImRect(rect.X + (rect.W - size) * 0.5f, rect.Y + (rect.H - size) * 0.5f, size, size);
	}
}
