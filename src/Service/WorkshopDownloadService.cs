using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Steamworks.Data;
using Steamworks.Ugc;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Keeps the next level in the playlist downloaded before the game needs it.
///     Left alone, the download starts when the lobby switches to the level - which happens
///     during the podium, so the podium sits there waiting for Steam. The lobby announces its
///     next level long before that, so the download can run while the player is still
///     driving.
///     The anchor is ZeepkistLobby.NextPlaylistIndex, which the host maintains and which
///     already accounts for random playlists and wrapping around the end. That makes this
///     work for a plain lobby playlist just as well as for ATH's own random levels - it never
///     has to know who put the level there.
///     It talks to Steam directly instead of going through WorkshopManager, and that is the
///     whole point. WorkshopManager.DownloadWorkshopLevel puts the item into a dictionary of
///     tracked downloads and only takes it out again once Steam reports it installed. While it
///     sits in there, the game's own ZeepkistNetwork.LoadLevelToSendToServer - which the server
///     triggers to have this client upload the level - calls TrackItemManually, an unguarded
///     Dictionary.Add on the same key. It throws, the upload sends an empty level data packet,
///     and the lobby skips the level. Prefetching the next level is exactly the case where both
///     happen to the same id at the same time, so the pre-download was reliably breaking the
///     level it was meant to speed up.
///     Steam does the deduplicating itself, so nothing is lost by not being tracked: an item
///     already installed and current returns immediately, and OnItemInstalled still fires, which
///     is what gets the level into LevelManager.
///     Downloading is not subscribing - this never subscribes the player to anything.
/// </summary>
public class WorkshopDownloadService
{
	public static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

	private readonly Dictionary<ulong, Task<bool>> _downloads = new();

	private PrefetchBehaviour _behaviour;
	private string _lastPrefetchedUid;

	public WorkshopDownloadService()
	{
		GameObject host = new(nameof(WorkshopDownloadService)) { hideFlags = HideFlags.HideAndDontSave };

		Object.DontDestroyOnLoad(host);
		_behaviour = host.AddComponent<PrefetchBehaviour>();
		_behaviour.Bind(this);
	}

	public void EnsureDownloaded(OnlineZeeplevel level)
	{
		_ = EnsureDownloadedAsync(level);
	}

	public Task<bool> EnsureDownloadedAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			return Task.FromResult(true);
		}

		if (_downloads.TryGetValue(level.WorkshopID, out Task<bool> running))
		{
			return running;
		}

		Task<bool> download = DownloadAsync(level.WorkshopID, level.Name);
		_downloads[level.WorkshopID] = download;

		return download;
	}

	public async Task WaitUntilReadyAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			return;
		}

		Task<bool> download = EnsureDownloadedAsync(level);

		using CancellationTokenSource timeout = new();

		if (await Task.WhenAny(download, Task.Delay(ReadyTimeout, timeout.Token)) == download)
		{
			timeout.Cancel();

			return;
		}

		Logger.LogWarning(
			$"WorkshopDownloadService: '{level.Name}' ({level.WorkshopID}) is still downloading after "
			+ $"{ReadyTimeout.TotalSeconds:F0}s, going ahead without it.");
	}

	public void PrefetchNextLevel()
	{
		OnlineZeeplevel next = GetNextLevel();

		if (next == null)
		{
			_lastPrefetchedUid = null;
			return;
		}

		if (next.UID == _lastPrefetchedUid)
		{
			return;
		}

		_lastPrefetchedUid = next.UID;
		Logger.LogInfo($"WorkshopDownloadService: Next up is '{next.Name}', making sure it is downloaded.");
		EnsureDownloaded(next);
	}

	private static OnlineZeeplevel GetNextLevel()
	{
		ZeepkistLobby lobby = ZeepkistNetwork.CurrentLobby;

		if (lobby?.Playlist == null || lobby.Playlist.Count == 0)
		{
			return null;
		}

		int index = lobby.NextPlaylistIndex;

		if (index < 0 || index >= lobby.Playlist.Count)
		{
			return null;
		}

		return lobby.Playlist[index];
	}

	private async Task<bool> DownloadAsync(ulong workshopId, string levelName)
	{
		try
		{
			Item? item = await Item.GetAsync(new PublishedFileId { Value = workshopId });

			if (item == null)
			{
				_downloads.Remove(workshopId);
				Logger.LogWarning($"WorkshopDownloadService: Steam does not know '{levelName}' ({workshopId}).");

				return false;
			}

			if (item.Value.IsInstalled && !item.Value.NeedsUpdate)
			{
				return true;
			}

			Logger.LogInfo($"WorkshopDownloadService: Pre-downloading '{levelName}' ({workshopId}).");

			if (await item.Value.DownloadAsync())
			{
				Logger.LogInfo($"WorkshopDownloadService: '{levelName}' ({workshopId}) is ready.");

				return true;
			}

			_downloads.Remove(workshopId);
			Logger.LogWarning($"WorkshopDownloadService: Could not pre-download '{levelName}' ({workshopId}).");

			return false;
		}
		catch (Exception e)
		{
			_downloads.Remove(workshopId);
			Logger.LogError(
				$"WorkshopDownloadService: Pre-download of '{levelName}' ({workshopId}) failed: {e.Message}");

			return false;
		}
	}

	public void Dispose()
	{
		if (_behaviour == null)
		{
			return;
		}

		Object.Destroy(_behaviour.gameObject);
		_behaviour = null;
	}
}
