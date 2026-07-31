using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using Newtonsoft.Json;
using ZeepkistNetworking;
using ZeepSDK.Playlist;
using Random = UnityEngine.Random;

namespace AuthorTimeHunting.Service;

public class LocalLevelCacheService
{
	private bool _initialized;


	private List<LevelItem> CachedLevelItems { get; } = new();

	public int LevelCount => CachedLevelItems.Count;

	/// <summary>
	///     Initializes the cache lazily on first use. Safe to call multiple times.
	/// </summary>
	public void EnsureInitialized()
	{
		if (_initialized)
		{
			return;
		}

		_initialized = true;
		ScanLocalPlaylists();
	}

	private void ScanLocalPlaylists()
	{
		Logger.LogInfo("LocalLevelCacheService: Scanning local playlists via PlaylistApi...");

		bool apiSuccess = TryScanViaApi();

		if (!apiSuccess || CachedLevelItems.Count == 0)
		{
			Logger.LogWarning(
				"LocalLevelCacheService: PlaylistApi scan failed or empty, falling back to manual file scan.");
			TryScanViaFileSystem();
		}

		if (CachedLevelItems.Count == 0)
		{
			Logger.LogWarning("LocalLevelCacheService: No levels found in any local playlist.");
			TryNotifyWarning("ATH: No levels found in local playlists. Add playlists to use ATH.");
		}
		else
		{
			Logger.LogInfo($"LocalLevelCacheService: Cached {CachedLevelItems.Count} unique levels.");
			TryNotifySuccess($"ATH: Loaded {CachedLevelItems.Count} levels from local playlists.");
		}
	}

	private bool TryScanViaApi()
	{
		try
		{
			IReadOnlyList<PlaylistSaveJSON> playlists = PlaylistApi.GetPlaylists();

			if (playlists == null || playlists.Count == 0)
			{
				Logger.LogWarning("LocalLevelCacheService: PlaylistApi returned no playlists.");
				return false;
			}

			HashSet<string> seenUids = new(StringComparer.OrdinalIgnoreCase);

			foreach (PlaylistSaveJSON playlist in playlists)
			{
				if (playlist?.levels == null)
				{
					continue;
				}

				foreach (OnlineZeeplevel level in playlist.levels)
					AddLevel(seenUids, level.UID, level.WorkshopID, level.Name, level.Author);
			}

			Logger.LogInfo(
				$"LocalLevelCacheService: PlaylistApi scan complete, {CachedLevelItems.Count} levels from {playlists.Count} playlists.");
			return true;
		}
		catch (Exception ex)
		{
			Logger.LogError($"LocalLevelCacheService: PlaylistApi scan failed: {ex.Message}");
			return false;
		}
	}

	private void TryScanViaFileSystem()
	{
		try
		{
			string playlistDir = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
				"Zeepkist", "Playlists");

			if (!Directory.Exists(playlistDir))
			{
				Logger.LogWarning($"LocalLevelCacheService: Playlist directory not found: {playlistDir}");
				return;
			}

			string[] files = Directory.GetFiles(playlistDir, "*.zeeplist", SearchOption.TopDirectoryOnly);

			if (files.Length == 0)
			{
				Logger.LogWarning($"LocalLevelCacheService: No .zeeplist files found in {playlistDir}");
				return;
			}

			HashSet<string> seenUids = new(StringComparer.OrdinalIgnoreCase);

			// Collect already cached UIDs to avoid duplicates with Api results
			foreach (LevelItem existing in CachedLevelItems) seenUids.Add(existing.FileUid);

			int fileCount = 0;

			foreach (string file in files)
				try
				{
					string json = File.ReadAllText(file);
					ZeeplistFile parsed = JsonConvert.DeserializeObject<ZeeplistFile>(json);

					if (parsed?.Levels == null)
					{
						continue;
					}

					foreach (ZeeplistLevel level in parsed.Levels)
					{
						ulong.TryParse(level.WorkshopID, out ulong workshopId);
						AddLevel(seenUids, level.UID, workshopId, level.Name, level.Author);
					}

					fileCount++;
				}
				catch (Exception ex)
				{
					Logger.LogWarning(
						$"LocalLevelCacheService: Failed to parse {Path.GetFileName(file)}: {ex.Message}");
				}

			Logger.LogInfo(
				$"LocalLevelCacheService: File system scan complete, parsed {fileCount}/{files.Length} files, {CachedLevelItems.Count} total levels.");
		}
		catch (Exception ex)
		{
			Logger.LogError($"LocalLevelCacheService: File system scan failed: {ex.Message}");
		}
	}

	private void AddLevel(HashSet<string> seenUids, string uid, ulong workshopId, string name, string author)
	{
		if (string.IsNullOrEmpty(uid))
		{
			return;
		}

		if (!seenUids.Add(uid))
		{
			return;
		}

		CachedLevelItems.Add(new LevelItem
		{
			FileUid = uid, WorkshopId = workshopId, Name = name, FileAuthor = author
		});
	}

	/// <summary>
	///     Returns a shuffled batch of cached levels, excluding the given UIDs.
	/// </summary>
	public List<LevelItem> GetRandomLevelItems(int count, IEnumerable<string> excludedUids = null)
	{
		EnsureInitialized();

		if (CachedLevelItems.Count == 0)
		{
			Logger.LogError("LocalLevelCacheService: No levels in cache.");
			return new List<LevelItem>();
		}

		HashSet<string> excluded = excludedUids != null
			? new HashSet<string>(excludedUids, StringComparer.OrdinalIgnoreCase)
			: new HashSet<string>();

		List<LevelItem> available = CachedLevelItems.Where(l => !excluded.Contains(l.FileUid)).ToList();

		if (available.Count == 0)
		{
			Logger.LogWarning("LocalLevelCacheService: All levels have been played. Playlist exhausted.");
			return new List<LevelItem>();
		}

		return available.OrderBy(_ => Random.value).Take(count).ToList();
	}

	private static void TryNotifySuccess(string message)
	{
		try
		{
			Messenger.Notify().LogSuccess(message);
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"LocalLevelCacheService: Could not send success notification: {ex.Message}");
		}
	}

	private static void TryNotifyWarning(string message)
	{
		try
		{
			Messenger.Notify().LogWarning(message);
		}
		catch (Exception ex)
		{
			Logger.LogWarning($"LocalLevelCacheService: Could not send warning notification: {ex.Message}");
		}
	}

	// JSON model for manual .zeeplist file parsing
	private class ZeeplistFile
	{
		public List<ZeeplistLevel> Levels { get; set; }
	}

	private class ZeeplistLevel
	{
		public string UID { get; set; }
		public string WorkshopID { get; set; }
		public string Name { get; set; }
		public string Author { get; set; }
	}
}