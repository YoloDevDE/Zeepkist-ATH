using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Steamworks.Data;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Pulls workshop levels down before the game needs them.
///     Without this the download only starts once the lobby switches to the level, which
///     happens during the podium - so the podium sits there waiting for Steam. ATH knows the
///     next level a whole round in advance, so it can pay that cost while the player is still
///     driving.
///     Downloading is not subscribing. WorkshopManager.DownloadWorkshopLevel only subscribes
///     when the player has turned on online_auto_subscribe themselves, and returns straight
///     away when the item is already installed and current.
/// </summary>
public class WorkshopDownloadService
{
	/// <summary>
	///     Workshop ids already handed to Steam this session. Steam handles a repeat request
	///     fine, but a level can be drawn again after a restart and there is no reason to ask
	///     twice.
	/// </summary>
	private readonly HashSet<ulong> _requested = new();

	/// <summary>
	///     Starts the download and returns immediately - the caller is mid-playlist-edit and
	///     must not wait for Steam. Failures are logged, never thrown: a missing pre-download
	///     costs time, it does not break the run.
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

			// Let a later draw of the same level try again.
			_requested.Remove(workshopId);
			Logger.LogWarning($"WorkshopDownloadService: Could not pre-download '{levelName}' ({workshopId}).");
		}
		catch (Exception e)
		{
			_requested.Remove(workshopId);
			Logger.LogError($"WorkshopDownloadService: Pre-download of '{levelName}' ({workshopId}) failed: {e.Message}");
		}
	}
}
