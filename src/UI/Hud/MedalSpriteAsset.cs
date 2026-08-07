using System;
using TMPro;
using UnityEngine;
using UnityEngine.TextCore;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The game's medals, as characters a TMP label can write.
///     The run ticker labelled its two rows "AT" and "G", which are the right two letters and the
///     wrong two things: the player already knows what an author medal looks like, and a letter
///     has to be read where a medal is recognised. TMP can draw a sprite inline with
///     <c>&lt;sprite index=0&gt;</c>, so the medals go in the text itself rather than beside it.
///     What TMP wants for that is a sprite asset, and a sprite asset wants one texture holding
///     every sprite in it. The game's medals are Sprites on whatever atlas the game put them on,
///     so they are blitted through a render texture into a strip of our own - through the GPU
///     rather than through <c>GetPixels</c>, because a texture shipped in a build is not readable
///     and asking it for its pixels throws.
///     Nothing here sets <c>faceInfo</c>, and that is deliberate: TMP falls back to the metrics of
///     the font the sprite is written into and scales the sprite to that font's ascent, so a medal
///     is exactly as tall as the capitals beside it at any font size, on any label.
/// </summary>
public class MedalSpriteAsset : IDisposable
{
	public const int Author = 0;

	public const int Gold = 1;

	/// <summary>
	///     The side of one cell in the strip. Medals are drawn a few dozen pixels tall in a HUD and
	///     this is the next power of two up from that, which is all the resolution the strip can
	///     usefully carry.
	/// </summary>
	private const int Cell = 128;

	private TMP_SpriteAsset _asset;

	private bool _built;

	public void Dispose()
	{
		if (_asset == null)
		{
			return;
		}

		Object.Destroy(_asset.spriteSheet);
		Object.Destroy(_asset.material);
		Object.Destroy(_asset);

		_asset = null;
	}

	/// <summary>
	///     Hands <paramref name="label" /> the sprites and says whether it got them. Everything here
	///     depends on the game having loaded its own art, so "no" is an ordinary answer and the
	///     caller is expected to have letters ready for it.
	/// </summary>
	public bool Install(TMP_Text label)
	{
		if (label == null)
		{
			return false;
		}

		TMP_SpriteAsset asset = Asset();

		if (asset == null)
		{
			return false;
		}

		label.spriteAsset = asset;

		return true;
	}

	/// <summary>
	///     The tag for one medal, tinted. <c>tint=1</c> is what makes the colour apply at all, and
	///     white leaves the medal its own colours - which is the point of using the art.
	/// </summary>
	public static string Tag(int medal, string hex)
	{
		return $"<sprite index={medal} tint=1 color={hex}>";
	}

	private TMP_SpriteAsset Asset()
	{
		if (_built)
		{
			return _asset;
		}

		Sprite author = GameSprites.AuthorMedal;
		Sprite gold = GameSprites.GoldMedal;

		if (author == null || gold == null)
		{
			return null;
		}

		_built = true;
		_asset = Build(author, gold);

		return _asset;
	}

	private static TMP_SpriteAsset Build(Sprite author, Sprite gold)
	{
		try
		{
			return Assemble(author, gold);
		}
		catch (Exception e)
		{
			Logger.LogWarning($"MedalSpriteAsset: The medals stay letters: {e.Message}");

			return null;
		}
	}

	/// <summary>
	///     The material goes on last, and that ordering is the whole feature.
	///     TMP_SpriteAsset.UpdateLookupTables opens with
	///     <c>if (material != null &amp;&amp; string.IsNullOrEmpty(m_Version)) UpgradeSpriteAsset()</c>,
	///     and an asset built at runtime satisfies both halves the moment it is handed a material:
	///     the version only ever gets written by that upgrade. UpgradeSpriteAsset then clears both
	///     tables and walks <c>spriteInfoList</c> - a plain public field with no initializer, so
	///     null on anything CreateInstance made. That is the NullReferenceException the medals used
	///     to die of, thrown from the spriteCharacterTable getter, which calls UpdateLookupTables
	///     itself while the lookups are still empty.
	///     With no material there is nothing to upgrade, so the tables survive and the lookups get
	///     built. Every later UpdateLookupTables inside TMP sits behind its own
	///     <c>if (lookup == null)</c> and never runs again, which is what makes handing the material
	///     over afterwards safe.
	/// </summary>
	private static TMP_SpriteAsset Assemble(Sprite author, Sprite gold)
	{
		Texture2D strip = Strip(author, gold);

		TMP_SpriteGlyph authorGlyph = Glyph(Author);
		TMP_SpriteGlyph goldGlyph = Glyph(Gold);

		TMP_SpriteAsset asset = ScriptableObject.CreateInstance<TMP_SpriteAsset>();

		asset.name = "ATH Medals";
		asset.hideFlags = HideFlags.HideAndDontSave;
		asset.spriteSheet = strip;
		asset.spriteGlyphTable.Add(authorGlyph);
		asset.spriteGlyphTable.Add(goldGlyph);

		asset.spriteCharacterTable.Add(Character(authorGlyph, "author"));
		asset.spriteCharacterTable.Add(Character(goldGlyph, "gold"));

		asset.UpdateLookupTables();

		asset.material = Material(strip);

		return asset;
	}

	/// <summary>One row of cells, the medals left to right in the order their indices name them.</summary>
	private static Texture2D Strip(Sprite author, Sprite gold)
	{
		Texture2D strip = new(Cell * 2, Cell, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };

		Paint(strip, author, Author);
		Paint(strip, gold, Gold);

		strip.Apply();

		return strip;
	}

	/// <summary>
	///     One medal into its cell. The blit's scale and offset cut the sprite out of whatever atlas
	///     the game keeps it on, and ReadPixels takes the result back off the GPU into the cell.
	/// </summary>
	private static void Paint(Texture2D strip, Sprite sprite, int cell)
	{
		Rect area = sprite.textureRect;
		Texture source = sprite.texture;

		Vector2 scale = new(area.width / source.width, area.height / source.height);
		Vector2 offset = new(area.x / source.width, area.y / source.height);

		RenderTexture buffer = RenderTexture.GetTemporary(Cell, Cell, 0, RenderTextureFormat.ARGB32);
		RenderTexture previous = RenderTexture.active;

		try
		{
			Graphics.Blit(source, buffer, scale, offset);
			RenderTexture.active = buffer;
			strip.ReadPixels(new Rect(0f, 0f, Cell, Cell), cell * Cell, 0);
		}
		finally
		{
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(buffer);
		}
	}

	private static Material Material(Texture2D strip)
	{
		Material material = new(SpriteShader()) { hideFlags = HideFlags.HideAndDontSave };

		material.SetTexture(ShaderUtilities.ID_MainTex, strip);

		return material;
	}

	/// <summary>
	///     Taken off TMP's own sprite asset where there is one. A shader nothing in the build
	///     references can be stripped out of it, and one that is already in use cannot be.
	/// </summary>
	private static Shader SpriteShader()
	{
		TMP_SpriteAsset defaults = TMP_Settings.defaultSpriteAsset;

		if (defaults != null && defaults.material != null)
		{
			return defaults.material.shader;
		}

		return Shader.Find("TextMeshPro/Sprite");
	}

	/// <summary>
	///     A square glyph that steps the cursor on by its own width, so a medal takes the room a
	///     wide letter would and the row after it starts where it started before.
	/// </summary>
	private static TMP_SpriteGlyph Glyph(int medal)
	{
		GlyphMetrics metrics = new(Cell, Cell, 0f, Cell, Cell);
		GlyphRect rect = new(medal * Cell, 0, Cell, Cell);

		return new TMP_SpriteGlyph((uint)medal, metrics, rect, 1f, 0);
	}

	/// <summary>
	///     0xFFFE is TMP's "this sprite has no character of its own": it is written by index or by
	///     name, never by typing something.
	/// </summary>
	private static TMP_SpriteCharacter Character(TMP_SpriteGlyph glyph, string name)
	{
		return new TMP_SpriteCharacter(0xFFFE, glyph) { name = name, scale = 1f };
	}
}
