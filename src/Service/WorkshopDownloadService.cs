using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks.Data;
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
///     Downloading is not subscribing. WorkshopManager.DownloadWorkshopLevel only subscribes
///     when the player has turned on online_auto_subscribe themselves, and returns straight
///     away when the item is already installed and current.
/// </summary>
public partial class WorkshopDownloadService
{
	/// <summary>
	///     How long a caller will wait for a level before going ahead without it. Long enough
	///     for a normal workshop level on a normal line, short enough that a dead Steam does not
	///     hang the run - going ahead early only risks the failure that used to be certain.
	/// </summary>
	public static readonly TimeSpan ReadyTimeout = TimeSpan.FromSeconds(30);

	/// <summary>
	///     Downloads handed to Steam this session, by workshop id, so a second caller joins the
	///     first one's download instead of starting another. Dropped again on failure so a later
	///     attempt can retry.
	/// </summary>
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

	/// <summary>
	///     Starts the download and returns immediately - callers are mid-playlist-edit or
	///     mid-frame and must not wait for Steam. Failures are logged, never thrown: a missed
	///     pre-download costs time, it does not break the run.
	/// </summary>
	public void EnsureDownloaded(OnlineZeeplevel level)
	{
		_ = EnsureDownloadedAsync(level);
	}

	/// <summary>
	///     The same download, as something that can be waited on. One task per workshop id: a
	///     second caller joins the first one's download rather than asking Steam twice.
	/// </summary>
	public Task<bool> EnsureDownloadedAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			// Levels from local playlists carry no workshop id - they are already on disk.
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

	/// <summary>
	///     Waits until the level is on disk, or until <see cref="ReadyTimeout" /> gives up on it.
	///     Nothing may switch the lobby to a level before this returns.
	///     The game loads a level off disk to send it to the server the moment the lobby moves
	///     to it. Doing that while Steam is still writing the item made the game's own loader
	///     throw ("An item with the same key has already been added"), which it reports by
	///     sending an empty level packet - the server then skips, ATH sees a level that is not
	///     the one it queued, calls it broken, draws a replacement, and does the whole thing
	///     again. A restarted run could burn through its level pool that way without ever
	///     loading a single level.
	/// </summary>
	public async Task WaitUntilReadyAsync(OnlineZeeplevel level)
	{
		if (level == null || level.WorkshopID == 0)
		{
			return;
		}

		Task<bool> download = EnsureDownloadedAsync(level);

		if (await Task.WhenAny(download, Task.Delay(ReadyTimeout)) == download)
		{
			return;
		}

		Logger.LogWarning(
			$"WorkshopDownloadService: '{level.Name}' ({level.WorkshopID}) is still downloading after "
			+ $"{ReadyTimeout.TotalSeconds:F0}s, going ahead without it.");
	}

	/// <summary>
	///     Looks at what the lobby says is coming next and makes sure it is on disk. Called
	///     every frame; does nothing unless the answer changed.
	/// </summary>
	private void PrefetchNextLevel()
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
			if (WorkshopManager.Instance == null)
			{
				Logger.LogWarning("WorkshopDownloadService: No WorkshopManager, skipping pre-download.");
				_downloads.Remove(workshopId);
				return false;
			}

			Logger.LogInfo($"WorkshopDownloadService: Pre-downloading '{levelName}' ({workshopId}).");

			// No ConfigureAwait(false): this is a game API and the continuation below touches
			// _downloads, which the main thread also writes.
			bool downloaded = await WorkshopManager.Instance.DownloadWorkshopLevel(new PublishedFileId { Value = workshopId });

			if (downloaded)
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
