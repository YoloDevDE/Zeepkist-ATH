using System.Collections.Generic;
using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     A short, centred message: the start countdown, a claimed medal, what the next level
///     is. The kind of thing that used to be written into the game's RoundOverText.
/// </summary>
public class OverlayBanner
{
	public OverlayBanner(string headline, IReadOnlyList<OverlayLine> lines, float displaySeconds)
	{
		Headline = headline;
		Lines = lines;
		DisplaySeconds = displaySeconds;
	}

	public string Headline { get; }
	public IReadOnlyList<OverlayLine> Lines { get; }

	/// <summary>Zero keeps the banner up until something replaces or clears it.</summary>
	public float DisplaySeconds { get; }

	public static OverlayBanner Of(string headline, float displaySeconds, params OverlayLine[] lines)
	{
		return new OverlayBanner(headline, lines, displaySeconds);
	}
}

public readonly struct OverlayLine
{
	public OverlayLine(string text, Color32 colour)
	{
		Text = text;
		Colour = colour;
	}

	public string Text { get; }
	public Color32 Colour { get; }

	public static OverlayLine Plain(string text)
	{
		return new OverlayLine(text, HudPalette.Default);
	}
}