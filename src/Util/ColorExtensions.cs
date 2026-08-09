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
	private static readonly StyleColors _sharedStyle = new();

	private static readonly ZeepkistColors _sharedZeepkist = new();

	extension(Color)
	{
		public static StyleColors Style => _sharedStyle;

		public static ZeepkistColors Zeepkist => _sharedZeepkist;

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

		public ActionColors Action { get; } = new();

		public SurfaceColors Surface { get; } = new();
	}

	/// <summary>What written text is worth: body, aside, heading, and the words that carry their own colour.</summary>
	public sealed class TextColors
	{
		public Color32 Default { get; } = new(230, 230, 230, 255);

		public Color32 Muted { get; } = new(153, 153, 153, 255);

		public Color32 Section { get; } = new(255, 212, 166, 255);

		public Color32 Command { get; } = new(122, 255, 122, 255);

		public Color32 LevelName { get; } = new(100, 210, 255, 255);

		public Color32 AuthorName { get; } = new(255, 215, 0, 255);

		/// <summary>The little word between two names, coloured so that both of them read as names.</summary>
		public Color32 Joiner { get; } = new(255, 150, 60, 255);
	}

	/// <summary>How a value or an outcome stands: fine, worth a look, or lost.</summary>
	public sealed class StatusColors
	{
		public Color32 Good { get; } = new(66, 179, 54, 255);

		public Color32 Warning { get; } = new(179, 179, 0, 255);

		public Color32 Danger { get; } = new(191, 57, 57, 255);

		public Color32 Positive { get; } = new(80, 228, 81, 255);

		public Color32 Negative { get; } = new(237, 178, 39, 255);

		public Color32 Bad { get; } = new(255, 90, 90, 255);

		public Color32 Alert { get; } = new(255, 0, 0, 255);

		public Color32 Fatal { get; } = new(15, 15, 15, 255);

		public Color32 FreeSkip { get; } = new(0, 255, 255, 255);

		public Color32 Penalty { get; } = new(191, 57, 57, 255);

		/// <summary>The medal is still in reach, but not by much.</summary>
		public Color32 Close { get; } = new(255, 226, 84, 255);
	}

	/// <summary>The accent a button carries, so the same action always looks the same.</summary>
	public sealed class ActionColors
	{
		public Color32 Skip { get; } = new(46, 104, 168, 255);

		public Color32 Broken { get; } = new(168, 106, 34, 255);

		public Color32 Pause { get; } = new(140, 118, 26, 255);

		public Color32 Resume { get; } = new(46, 132, 60, 255);

		public Color32 Restart { get; } = new(78, 78, 122, 255);

		public Color32 Stop { get; } = new(150, 46, 46, 255);
	}

	/// <summary>What is drawn behind the text: panel backgrounds, bar tracks, unlit things, plain white.</summary>
	public sealed class SurfaceColors
	{
		public Color32 Panel { get; } = new(12, 14, 18, 214);

		public Color32 Track { get; } = new(255, 255, 255, 38);

		public Color32 White { get; } = new(255, 255, 255, 255);

		/// <summary>What a screen that takes the whole game over puts over the game.</summary>
		public Color32 Backdrop { get; } = new(8, 9, 12, 242);

		/// <summary>
		///     The same, thinned until a picture behind it is still a picture. Anything darker and
		///     the backdrop might as well be flat; anything lighter and the words stop being words.
		/// </summary>
		public Color32 Veil { get; } = new(8, 9, 12, 176);

		/// <summary>
		///     The same, for a screen the player still has to see past - the podium behind the level
		///     summary is where "press Y" is written, and covering that up would strand them.
		/// </summary>
		public Color32 Shade { get; } = new(8, 9, 12, 208);

		/// <summary>A tile lifted off that backdrop, and the same tile under the pointer.</summary>
		public Color32 Tile { get; } = new(255, 255, 255, 20);

		public Color32 TileHovered { get; } = new(255, 255, 255, 48);

		/// <summary>
		///     A button that is a button: opaque, so the picture behind the menu stops showing
		///     through the thing you are supposed to press. The tile colours above are a wash over
		///     whatever is behind them, which is right for a panel and wrong for a menu over art -
		///     six translucent rectangles over a photograph read as six lighter patches of
		///     photograph.
		/// </summary>
		public Color32 Button { get; } = new(30, 33, 40, 255);

		public Color32 ButtonHovered { get; } = new(46, 50, 60, 255);

		public Color32 ButtonPressed { get; } = new(20, 22, 27, 255);

		/// <summary>The hairline that keeps a dark button off a dark backdrop.</summary>
		public Color32 Outline { get; } = new(255, 255, 255, 40);

		/// <summary>A lamp that is not lit yet: visible enough to be counted, dark enough to be off.</summary>
		public Color32 Unlit { get; } = new(64, 64, 64, 255);
	}

	/// <summary>Everything the game itself gives a colour to.</summary>
	public sealed class ZeepkistColors
	{
		public MedalColors Medal { get; } = new();
	}

	/// <summary>The colours the game gives its medals.</summary>
	public sealed class MedalColors
	{
		public Color32 Author { get; } = new(134, 56, 147, 255);

		public Color32 Gold { get; } = new(255, 214, 0, 255);
	}
}
