using System;
using System.Collections.Generic;
using AuthorTimeHunting.UI.Toolkit;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The game's medals, cut out once into textures of the mod's own.
///     The panels used to draw <see cref="GameSprites" /> straight, a Sprite off PlayerManager
///     handed to Imui every frame. That works for exactly as long as the level the mod started on:
///     the medals sit on an atlas the game loads with its adventure content, and the second level
///     load takes the sprite out from under the drawer - so every medal in the bar turned into the
///     coloured dot that is meant for the first frame of a session, and stayed that way.
///     A copy has no such owner. Each medal is blitted out of whatever atlas the game keeps it on
///     into a texture flagged <see cref="HideFlags.HideAndDontSave" />, which no scene load can
///     take away, and the panels draw the copy. It goes through the GPU rather than
///     <c>GetPixels</c> because a texture shipped in a build is not readable and asking it for its
///     pixels throws.
///     The copy is taken on the first frame a medal is asked for and the game has one. Before that
///     the answer is null and the caller draws its dot, which is what the dot was always for.
/// </summary>
public class MedalArt : IDisposable
{
	/// <summary>
	///     The side of one copy. Medals are drawn a few dozen pixels wide in a bar, and this is the
	///     next power of two up from that - all the resolution a copy can usefully carry.
	/// </summary>
	private const int _size = 128;

	private readonly Dictionary<UiMedal, Texture2D> _copies = new();

	/// <summary>
	///     Set once a copy has been attempted and thrown. The sprite being missing is an ordinary
	///     answer worth asking again next frame; the blit failing is not going to start working, and
	///     retrying it is a warning per frame for the rest of the session.
	/// </summary>
	private bool _failed;

	public Texture2D Author => Copy(UiMedal.Author, GameSprites.AuthorMedal);

	public Texture2D Gold => Copy(UiMedal.Gold, GameSprites.GoldMedal);

	public Texture2D YouTried => Copy(UiMedal.YouTried, GameSprites.YouTriedMedal);

	public void Dispose()
	{
		foreach (Texture2D copy in _copies.Values)
		{
			Object.Destroy(copy);
		}

		_copies.Clear();
	}

	private Texture2D Copy(UiMedal medal, Sprite sprite)
	{
		if (_copies.TryGetValue(medal, out Texture2D done) && done != null)
		{
			return done;
		}

		if (_failed || sprite == null)
		{
			return null;
		}

		Texture2D copy = Cut(sprite);

		if (copy == null)
		{
			return null;
		}

		_copies[medal] = copy;

		return copy;
	}

	private Texture2D Cut(Sprite sprite)
	{
		try
		{
			return Blit(sprite);
		}
		catch (Exception e)
		{
			_failed = true;
			Logger.LogWarning($"MedalArt: The medals stay dots: {e.Message}");

			return null;
		}
	}

	/// <summary>
	///     The blit's scale and offset cut the sprite out of the atlas it shares, and ReadPixels
	///     takes the result back off the GPU into a texture the mod owns.
	/// </summary>
	private static Texture2D Blit(Sprite sprite)
	{
		Rect area = sprite.textureRect;
		Texture source = sprite.texture;

		Vector2 scale = new(area.width / source.width, area.height / source.height);
		Vector2 offset = new(area.x / source.width, area.y / source.height);

		Texture2D copy = new(_size, _size, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };

		RenderTexture buffer = RenderTexture.GetTemporary(_size, _size, 0, RenderTextureFormat.ARGB32);
		RenderTexture previous = RenderTexture.active;

		try
		{
			Graphics.Blit(source, buffer, scale, offset);
			RenderTexture.active = buffer;
			copy.ReadPixels(new Rect(0f, 0f, _size, _size), 0, 0);
			copy.Apply();
		}
		finally
		{
			RenderTexture.active = previous;
			RenderTexture.ReleaseTemporary(buffer);
		}

		return copy;
	}
}
