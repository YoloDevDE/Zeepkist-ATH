using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     Level thumbnails, as something a drawer can ask for without waiting.
///     The game loads these off disk asynchronously, and a GUI pass has nowhere to await. So
///     the first ask starts the load and returns nothing, and the picture appears a frame or
///     two later - which for a screen the player is reading is soon enough.
/// </summary>
internal static class LevelThumbnails
{
	/// <summary>Thumbnails already loaded. Null means "asked for and there is none".</summary>
	private static readonly Dictionary<string, Texture2D> Loaded = new();

	/// <summary>Level ids currently being loaded, so a redraw does not start the load again.</summary>
	private static readonly HashSet<string> Pending = new();

	/// <summary>
	///     The thumbnail for a level, or null while it is still coming - and permanently null
	///     for a level that has none. Callers are expected to draw a placeholder for null rather
	///     than wait.
	/// </summary>
	public static Texture2D Get(string levelUid)
	{
		if (string.IsNullOrEmpty(levelUid) || ThumbnailManager.Instance == null)
		{
			return null;
		}

		if (Loaded.TryGetValue(levelUid, out Texture2D cached))
		{
			return cached;
		}

		// The game keeps its own cache of what it has already shown; a level the player has
		// just played is usually in it, and then there is nothing to load at all.
		if (ThumbnailManager.Instance.TryGetLevelThumbnail(levelUid, out Texture2D ready) && ready != null)
		{
			Loaded[levelUid] = ready;

			return ready;
		}

		if (Pending.Add(levelUid))
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

			// Stored even when null: a level without a thumbnail must not be asked for again on
			// every frame the report is open.
			Loaded[levelUid] = thumbnail;
		}
		catch (Exception e)
		{
			Loaded[levelUid] = null;
			Logger.LogWarning($"LevelThumbnails: Could not load the thumbnail for '{levelUid}': {e.Message}");
		}
		finally
		{
			Pending.Remove(levelUid);
		}
	}
}
