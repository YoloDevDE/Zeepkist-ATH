using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Level thumbnails, as something a drawer can ask for without waiting.
///     The game loads these off disk asynchronously, and a GUI pass has nowhere to await. So
///     the first ask starts the load and returns nothing, and the picture appears a frame or
///     two later - which for a screen the player is reading is soon enough.
/// </summary>
public static class LevelThumbnails
{
	private static readonly Dictionary<string, Texture2D> _loaded = new();

	private static readonly HashSet<string> _pending = new();

	public static Texture2D Get(string levelUid)
	{
		if (string.IsNullOrEmpty(levelUid) || ThumbnailManager.Instance == null)
		{
			return null;
		}

		if (_loaded.TryGetValue(levelUid, out Texture2D cached))
		{
			return cached;
		}

		if (ThumbnailManager.Instance.TryGetLevelThumbnail(levelUid, out Texture2D ready) && ready != null)
		{
			_loaded[levelUid] = ready;

			return ready;
		}

		if (_pending.Add(levelUid))
		{
			_ = LoadAsync(levelUid);
		}

		return null;
	}

	private static async Task LoadAsync(string levelUid)
	{
		try
		{
			Texture2D thumbnail = await ThumbnailManager.Instance.GetLevelThumbnailAsync(levelUid);

			_loaded[levelUid] = thumbnail;
		}
		catch (Exception e)
		{
			_loaded[levelUid] = null;
			Logger.LogWarning($"LevelThumbnails: Could not load the thumbnail for '{levelUid}': {e.Message}");
		}
		finally
		{
			_pending.Remove(levelUid);
		}
	}
}
