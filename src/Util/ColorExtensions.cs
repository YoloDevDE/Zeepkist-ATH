using UnityEngine;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Every colour the mod draws, in one place, hung off <see cref="Color" /> itself so a call
///     site reads as one chain: <c>Color.Style.Status.Danger</c>, <c>Color.Zeepkist.Medal.Author</c>.
///     Groups are picked by what a thing means, the way a document picks Heading over
///     "bold, 18pt".
///     This file breaks the repo's rules on purpose: nested types and many types in one file are
///     what keeps the whole palette readable as a single table. It is the only such exception.
/// </summary>
public static class ColorExtensions
{
	private static readonly StyleColors SharedStyle = new();

	private static readonly ZeepkistColors SharedZeepkist = new();

	extension(Color)
	{
		public static StyleColors Style => SharedStyle;

		public static ZeepkistColors Zeepkist => SharedZeepkist;

		/// <summary>
		///     Reads <c>RRGGBB</c> or <c>RRGGBBAA</c>, with or without a leading <c>#</c>.
		///     Anything unreadable becomes magenta, so a typo shows up on screen.
		/// </summary>
		public static Color FromHex(string hex)
		{
			if (string.IsNullOrWhiteSpace(hex))
			{
				return Color.magenta;
			}

			string trimmed = hex.Trim();
			string html = trimmed.StartsWith("#") ? trimmed : "#" + trimmed;

			if (!ColorUtility.TryParseHtmlString(html, out Color parsed))
			{
				return Color.magenta;
			}

			return parsed;
		}
	}

	/// <summary>The mod's own palette.</summary>
	public sealed class StyleColors
	{
		public TextColors Text { get; } = new();

		public StatusColors Status { get; } = new();

		public PaceColors Pace { get; } = new();

		public ActionColors Action { get; } = new();

		public SurfaceColors Surface { get; } = new();
	}

	/// <summary>What written text is worth: body, aside, heading, and the words that carry their own colour.</summary>
	public sealed class TextColors
	{
		public Color Default => new Color32(230, 230, 230, 255);

		public Color Muted => new Color32(153, 153, 153, 255);

		public Color Section => new Color32(255, 212, 166, 255);

		public Color Heading => new Color32(179, 54, 163, 255);

		public Color Key => new Color32(127, 219, 255, 255);

		public Color Command => new Color32(122, 255, 122, 255);

		public Color Info => new Color32(170, 170, 170, 255);

		public Color LevelName => new Color32(100, 210, 255, 255);

		public Color AuthorName => new Color32(255, 215, 0, 255);
	}

	/// <summary>How a value or an outcome stands: fine, worth a look, or lost.</summary>
	public sealed class StatusColors
	{
		public Color Good => new Color32(66, 179, 54, 255);

		public Color Warning => new Color32(179, 179, 0, 255);

		public Color Danger => new Color32(191, 57, 57, 255);

		public Color Positive => new Color32(80, 228, 81, 255);

		public Color Negative => new Color32(237, 178, 39, 255);

		public Color Bad => new Color32(255, 90, 90, 255);

		public Color Alert => new Color32(255, 0, 0, 255);

		public Color Fatal => new Color32(15, 15, 15, 255);

		public Color FreeSkip => new Color32(0, 255, 255, 255);

		public Color Penalty => new Color32(191, 57, 57, 255);
	}

	/// <summary>How much of the author time is left, from comfortable to gone.</summary>
	public sealed class PaceColors
	{
		public Color Safe => new Color32(255, 255, 255, 255);

		public Color Close => new Color32(255, 226, 84, 255);

		public Color Lost => new Color32(255, 146, 48, 255);

		public Color Critical => new Color32(255, 74, 74, 255);

		public Color Gone => new Color32(150, 40, 40, 255);
	}

	/// <summary>The accent a button carries, so the same action always looks the same.</summary>
	public sealed class ActionColors
	{
		public Color Skip => new Color32(46, 104, 168, 255);

		public Color Broken => new Color32(168, 106, 34, 255);

		public Color Pause => new Color32(140, 118, 26, 255);

		public Color Resume => new Color32(46, 132, 60, 255);

		public Color Restart => new Color32(78, 78, 122, 255);

		public Color Stop => new Color32(150, 46, 46, 255);
	}

	/// <summary>What is drawn behind the text: panel backgrounds, bar tracks, unlit things, plain white.</summary>
	public sealed class SurfaceColors
	{
		public static Color Panel => new Color32(12, 14, 18, 214);

		public static Color Track => new Color32(255, 255, 255, 38);

		public Color White => new Color32(255, 255, 255, 255);

		/// <summary>What a screen that takes the whole game over puts over the game.</summary>
		public Color Backdrop => new Color32(8, 9, 12, 242);

		/// <summary>
		///     The same, for a screen the player still has to see past - the podium behind the level
		///     summary is where "press Y" is written, and covering that up would strand them.
		/// </summary>
		public Color Shade => new Color32(8, 9, 12, 208);

		/// <summary>A tile lifted off that backdrop, and the same tile under the pointer.</summary>
		public Color Tile => new Color32(255, 255, 255, 20);

		public Color TileHovered => new Color32(255, 255, 255, 48);

		/// <summary>A lamp that is not lit yet: visible enough to be counted, dark enough to be off.</summary>
		public Color Unlit => new Color32(64, 64, 64, 255);
	}

	/// <summary>Everything the game itself gives a colour to.</summary>
	public sealed class ZeepkistColors
	{
		public MedalColors Medal { get; } = new();
	}

	/// <summary>The colours the game gives its medals.</summary>
	public sealed class MedalColors
	{
		public Color Author => new Color32(134, 56, 147, 255);

		public Color Gold => new Color32(255, 214, 0, 255);
	}
}
