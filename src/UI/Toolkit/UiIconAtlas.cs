using System;
using System.IO;
using System.Reflection;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     The button icons, as one strip of white on transparent: one square cell per
///     <see cref="UiIcon" />, in that enum's order.
///     The icons are Bootstrap Icons, baked into the strip by tools/iconatlas.py and embedded in
///     the plugin, so there is nothing to install and nothing to download. They were drawn here in
///     canvas primitives before that - triangles, bars and rectangles - and the trouble with
///     drawing your own icons is that a square fills its box and a triangle fills half of it, so
///     five buttons in a row carried five different amounts of ink and looked it. A set drawn as a
///     set does not have that problem.
///     Static because <see cref="UiIcons" /> is: an icon is asked for by a dozen widgets from
///     static draw code, and threading one instance through all of them would be a lot of
///     plumbing for one texture. The texture is the mod's own, so it is destroyed on the way out.
/// </summary>
public static class UiIconAtlas
{
	private const string _resource = "AuthorTimeHunting.Icons.png";

	/// <summary>How many cells the strip has. One per <see cref="UiIcon" /> except None.</summary>
	private const int _cells = 8;

	private static Texture2D _strip;

	private static bool _tried;

	public static Texture2D Texture
	{
		get
		{
			if (_tried)
			{
				return _strip;
			}

			_tried = true;
			_strip = Load();

			return _strip;
		}
	}

	/// <summary>
	///     Where this icon sits on the strip, in the scale-then-offset form Imui hands the shader:
	///     the cell's width as a fraction of the strip, and its left edge in the same units.
	/// </summary>
	public static Vector4 Cell(UiIcon icon)
	{
		int index = Mathf.Clamp((int)icon - 1, 0, _cells - 1);

		return new Vector4(1f / _cells, 1f, index / (float)_cells, 0f);
	}

	public static void Release()
	{
		if (_strip == null)
		{
			return;
		}

		Object.Destroy(_strip);
		_strip = null;
		_tried = false;
	}

	private static Texture2D Load()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(_resource);

			if (stream == null)
			{
				Logger.LogWarning($"UiIconAtlas: '{_resource}' is not in the plugin. The buttons keep their words.");

				return null;
			}

			byte[] png = new byte[stream.Length];
			stream.Read(png, 0, png.Length);

			Texture2D strip = new(2, 2, TextureFormat.RGBA32, false)
			{
				hideFlags = HideFlags.HideAndDontSave, wrapMode = TextureWrapMode.Clamp, filterMode = FilterMode.Bilinear
			};

			strip.LoadImage(png);

			return strip;
		}
		catch (Exception e)
		{
			Logger.LogWarning($"UiIconAtlas: Could not load the icons: {e.Message}");

			return null;
		}
	}
}
