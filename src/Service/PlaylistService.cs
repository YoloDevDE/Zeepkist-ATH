using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.Service;

public class PlaylistService
{
	private readonly WorkshopDownloadService _workshopDownloads;

	public PlaylistService(WorkshopDownloadService workshopDownloads)
	{
		_workshopDownloads = workshopDownloads;
	}

	private static List<OnlineZeeplevel> CurrentLobbyPlaylist => ZeepkistNetwork.CurrentLobby.Playlist;
	private static int CurrentPlaylistIndex => ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;

	// A Playlist cant be empty
	public void StartNewPlaylist(OnlineZeeplevel initialLevel)
	{
		CurrentLobbyPlaylist.Clear();
		CurrentLobbyPlaylist.Add(initialLevel);

		// The lobby is still pointing wherever the last run left it. After a run that played
		// three levels that is index 2, and the playlist it indexes into is now one entry
		// long - every read of Playlist[CurrentPlaylistIndex] throws until the server's own
		// skip catches up. That window was long enough to kill the second run of a session
		// the moment its first level finished loading. The /fs 0 that follows moves the
		// server; this moves us, now, so nothing has to survive the gap.
		ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = 0;

		_workshopDownloads.EnsureDownloaded(initialLevel);
		QueueServerPlaylistUpdate();
	}

	public void AddLevelToCurrentPlaylist(OnlineZeeplevel level)
	{
		PlaylistItem playlistItem = new(level.UID, level.WorkshopID, level.Name, level.Author);
		MultiplayerApi.AddLevelToPlaylist(playlistItem, true);
		Logger.LogInfo(
			$"PlaylistService: Added level '{level.Name}' (UID: {level.UID}) to playlist. Playlist count is now {CurrentLobbyPlaylist.Count}.");
		// This is the level the run will skip to next round. Fetch it now, while the player
		// is still driving, instead of letting the podium wait for Steam.
		_workshopDownloads.EnsureDownloaded(level);
		QueueServerPlaylistUpdate();
	}

	public void ReplaceLevelInCurrentPlaylist(OnlineZeeplevel oldLevel, OnlineZeeplevel newLevel)
	{
		int index = CurrentLobbyPlaylist.FindIndex(old => old.UID == oldLevel.UID);

		if (index == -1)
		{
			Logger.LogWarning($"PlaylistService: Could not find level with UID {oldLevel.UID} in playlist");
			return;
		}

		CurrentLobbyPlaylist[index] = newLevel;
		Logger.LogInfo($"PlaylistService: Replaced level '{oldLevel.Name}' with '{newLevel.Name}' at index {index}");
		_workshopDownloads.EnsureDownloaded(newLevel);
		QueueServerPlaylistUpdate();
	}

	#region ServerPlaylistUpdateQueue

	// A playlist can only be pushed to the server once every 5 seconds. To respect that limit
	// without blocking callers, server updates are queued and drained by a single async loop
	// that keeps a minimum interval between consecutive updates.
	//
	// The loop is asynchronous but NOT concurrent: it runs on Unity's main thread throughout,
	// because MultiplayerApi.UpdateServerPlaylist may only be called from there. _queueLock
	// is therefore uncontended today and kept purely as a guard for future callers.
	private static readonly TimeSpan MinUpdateInterval = TimeSpan.FromSeconds(5);

	private readonly Queue<Action> _updateQueue = new();
	private readonly object _queueLock = new();
	private Task _processingTask;
	private DateTime _lastUpdate = DateTime.MinValue;

	/// <summary>
	///     Queues a server playlist update. Returns immediately; the update is applied
	///     asynchronously while respecting the 5 second rate limit. The returned task
	///     completes once the whole queue (including this update) has been processed,
	///     so callers may optionally await it before continuing.
	/// </summary>
	private Task QueueServerPlaylistUpdate()
	{
		return Enqueue(MultiplayerApi.UpdateServerPlaylist);
	}


	private Task Enqueue(Action update)
	{
		lock (_queueLock)
		{
			_updateQueue.Enqueue(update);
			Logger.LogInfo(
				$"PlaylistService: Enqueued server playlist update. Queue length is now {_updateQueue.Count}.");

			if (_processingTask == null || _processingTask.IsCompleted)
			{
				Logger.LogInfo("PlaylistService: Starting queue processing task.");
				_processingTask = ProcessQueueAsync();
			}
			else
			{
				Logger.LogInfo("PlaylistService: Queue processing task already running, update appended.");
			}

			return _processingTask;
		}
	}

	private async Task ProcessQueueAsync()
	{
		// Yield before touching the queue: Enqueue starts this loop from inside its lock,
		// and the update below must not run synchronously in there. The continuation
		// comes back on Unity's SynchronizationContext - see the comment at update().
		await Task.Yield();

		Logger.LogInfo("PlaylistService: Queue processing loop started.");

		while (true)
		{
			Action update;

			lock (_queueLock)
			{
				if (_updateQueue.Count == 0)
				{
					Logger.LogInfo("PlaylistService: Queue empty, stopping processing loop.");
					_processingTask = null;
					return;
				}

				update = _updateQueue.Dequeue();
				Logger.LogInfo($"PlaylistService: Dequeued update. {_updateQueue.Count} update(s) remaining in queue.");
			}

			TimeSpan sinceLastUpdate = DateTime.UtcNow - _lastUpdate;

			if (sinceLastUpdate < MinUpdateInterval)
			{
				TimeSpan waitTime = MinUpdateInterval - sinceLastUpdate;
				Logger.LogInfo(
					$"PlaylistService: Rate limit active, waiting {waitTime.TotalSeconds:F1}s before applying update.");
				await Task.Delay(waitTime);
			}

			try
			{
				// MultiplayerApi.UpdateServerPlaylist touches lobby state and has to run on
				// Unity's main thread. This loop used to be started via Task.Run, which put
				// every single update on a thread pool thread instead.
				Logger.LogInfo("PlaylistService: Applying server playlist update now.");
				update();
				Logger.LogInfo("PlaylistService: Server playlist update applied successfully.");
			}
			catch (Exception ex)
			{
				Logger.LogError($"PlaylistService: Server playlist update failed: {ex.Message}");
			}

			_lastUpdate = DateTime.UtcNow;
		}
	}

	#endregion


	#region SkipLevelCommands

	public void SkipLevel()
	{
		SkipCommand();
	}

	public void SkipToNextLevel()
	{
		SkipCommand("next");
	}

	public void SkipToPrevLevel()
	{
		SkipCommand("prev");
	}

	public void SkipToLastLevel()
	{
		SkipCommand(CurrentLobbyPlaylist.Count - 1);
	}

	public void SkipToFirstLevel()
	{
		SkipCommand(0);
	}

	public void RestartCurrentLevel()
	{
		SkipCommand("restart");
	}

	private static void SkipCommand(int command)
	{
		ChatApi.SendMessage($"/fs {command}");
	}

	private static void SkipCommand(string command = null)
	{
		if (command == null)
		{
			ChatApi.SendMessage("/fs");
			return;
		}

		ChatApi.SendMessage($"/fs {command}");
	}

	#endregion
}
