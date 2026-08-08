using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Hud;

/// <summary>
///     The mod's own logo, loaded once from the plugin's resources and shared by everyone who
///     draws it: the loading screen while a hunt is being set up, and the bar once it is running.
///     It used to be loaded and owned by the loading screen alone. The bar wants the same image,
///     and two screens each carrying their own copy of the same PNG is one copy too many - so the
///     texture lives here, loaded on the first ask and dropped once with the session.
/// </summary>
public class AthThumbnail : IDisposable
{
	private const string Resource = "AuthorTimeHunting.Thumbnail.png";

	private Texture2D _texture;

	private bool _tried;

	public Texture2D Texture
	{
		get
		{
			if (_tried)
			{
				return _texture;
			}

			_tried = true;
			_texture = Load();

			return _texture;
		}
	}

	public void Dispose()
	{
		if (_texture == null)
		{
			return;
		}

		Object.Destroy(_texture);
		_texture = null;
	}

	private static Texture2D Load()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(Resource);

			if (stream == null)
			{
				Logger.LogWarning($"AthThumbnail: '{Resource}' is not in the plugin.");

				return null;
			}

			byte[] png = new byte[stream.Length];
			stream.Read(png, 0, png.Length);

			Texture2D texture = new(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
			texture.LoadImage(png);

			return texture;
		}
		catch (Exception e)
		{
			Logger.LogWarning($"AthThumbnail: Could not load the thumbnail: {e.Message}");

			return null;
		}
	}
}
