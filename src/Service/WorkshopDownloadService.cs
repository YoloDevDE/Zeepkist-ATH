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
	///     Workshop ids already handed to Steam this session. Dropped again on failure so a
	///     later attempt can retry.
	/// </summary>
	private readonly HashSet<ulong> _requested = new();

	private PrefetchBehaviour _behaviour;
	private string _lastPrefetchedUid;

	public WorkshopDownloadService()
	{
		GameObject host = new(nameof(WorkshopDownloadService))
		{
			hideFlags = HideFlags.HideAndDontSave
		};

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
		if (level == null || level.WorkshopID == 0)
		{
			// Levels from local playlists carry no workshop id - they are already on disk.
			return;
		}

		if (!_requested.Add(level.WorkshopID))
		{
			return;
		}

		_ = DownloadAsync(level.WorkshopID, level.Name);
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

	private async Task DownloadAsync(ulong workshopId, string levelName)
	{
		try
		{
			if (WorkshopManager.Instance == null)
			{
				Logger.LogWarning("WorkshopDownloadService: No WorkshopManager, skipping pre-download.");
				_requested.Remove(workshopId);
				return;
			}

			Logger.LogInfo($"WorkshopDownloadService: Pre-downloading '{levelName}' ({workshopId}).");

			// No ConfigureAwait(false): this is a game API and the continuation below touches
			// _requested, which the main thread also writes.
			bool downloaded = await WorkshopManager.Instance.DownloadWorkshopLevel(new PublishedFileId
			{
				Value = workshopId
			});

			if (downloaded)
			{
				Logger.LogInfo($"WorkshopDownloadService: '{levelName}' ({workshopId}) is ready.");
				return;
			}

			_requested.Remove(workshopId);
			Logger.LogWarning($"WorkshopDownloadService: Could not pre-download '{levelName}' ({workshopId}).");
		}
		catch (Exception e)
		{
			_requested.Remove(workshopId);
			Logger.LogError($"WorkshopDownloadService: Pre-download of '{levelName}' ({workshopId}) failed: {e.Message}");
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
