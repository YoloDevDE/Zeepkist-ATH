using TMPro;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     Picks a character the label in front of us can actually draw.
///     A font asset draws what was baked into its atlas and nothing else. The HUD is set in Code
///     New Roman, which has no U+25CF, so the start lamps came out as three hollow boxes and every
///     frame of every countdown wrote
///     <c>
///         The character with Unicode value ● was not found in the
///         [Code New Roman b SDF] font asset or any potential fallbacks
///     </c>
///     into the player log.
///     Lending the font a fallback was the obvious fix and it does not work here: no font asset
///     the game has open carries the glyph either, and a font asset built at runtime from an OS
///     font dies on <c>Unable to load font face for [Arial]</c> - a built player has no font data
///     to load, only the name of a system font.
///     So nothing is imported. The shape is chosen from a list of shapes instead, best first, and
///     the font itself says which one it can draw. The last entry in that list is the contract:
///     make it something every font on earth has, and there is no case left where a box appears.
/// </summary>
public class GlyphChoice
{
	/// <summary>
	///     The first of <paramref name="candidates" /> that <paramref name="label" /> can draw, or
	///     the last of them if it can draw none - a list whose last entry is a plain letter cannot
	///     come back empty.
	/// </summary>
	public static string First(TMP_Text label, string[] candidates)
	{
		string last = candidates[candidates.Length - 1];
		TMP_FontAsset font = label == null ? null : label.font;

		if (font == null)
		{
			return last;
		}

		string drawable = Drawable(font, candidates);

		Logger.LogInfo($"GlyphChoice: {font.name} draws '{drawable ?? last}'.");

		return drawable ?? last;
	}

	private static string Drawable(TMP_FontAsset font, string[] candidates)
	{
		foreach (string candidate in candidates)
		{
			if (!font.HasCharacters(candidate))
			{
				continue;
			}

			return candidate;
		}

		return null;
	}
}
