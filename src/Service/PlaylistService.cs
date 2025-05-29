using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.Service;

public class PlaylistService
{
    private PlaylistService()
    {
    }

    public static PlaylistService Instance { get; } = new PlaylistService();
    public OnlineZeeplevel CurrentBrokenZeeplevel { get; set; }

    private List<OnlineZeeplevel> CachedOnlineZeeplevels { get; set; } = new List<OnlineZeeplevel>();


    public OnlineZeeplevel GetCurrentZeepkistNetworkPlaylistLevel => ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex];

    private int CurrentPlaylistIndex()
    {
        return Math.Max(0, CachedOnlineZeeplevels.Count - 2);
    }

    private int NextPlaylistIndex()
    {
        return
            CachedOnlineZeeplevels.Count == 0
                ? 0
                : (CurrentPlaylistIndex() + 1) % CachedOnlineZeeplevels.Count;
    }


    public async Task StartNewPlaylist()
    {
        CachedOnlineZeeplevels = new List<OnlineZeeplevel>();
        await PopulatePlaylist();
    }

    public void SkipLevel()
    {
        ChatApi.SendMessage("/fs");
    }

    public void SkipToLastLevel()
    {
        ChatApi.SendMessage($"/fs {ZeepkistNetwork.CurrentLobby.Playlist.Count - 1}");
    }

    public async Task PopulatePlaylist()
    {
        Logger.LogInfo("PlaylistService: Starting playlist population");

        try
        {
            // Wait until the GameState is not 0
            Logger.LogDebug("PlaylistService: Waiting for GameState to change from 0 before updating playlist");
            await WaitUntilGameStateNotZero();
            Logger.LogInfo($"PlaylistService: GameState is now {ZeepkistNetwork.CurrentLobby.GameState}, proceeding with playlist update");

            // Log the current state of the playlist
            Logger.LogDebug($"PlaylistService: Current playlist has {CachedOnlineZeeplevels.Count} levels before adding new level");

            // Get a new random level and log details
            LevelItem levelItem = RandomLevelService.Instance.GetRandomLevelItem();
            Logger.LogInfo($"PlaylistService: Adding new level to playlist: '{levelItem.Name}' (UID: {levelItem.FileUid})");

            // Convert to OnlineZeepLevel and add to cache
            OnlineZeeplevel onlineLevel = levelItem.ToOnlineZeepLevel();
            CachedOnlineZeeplevels.Add(onlineLevel);
            Logger.LogDebug($"PlaylistService: Successfully added level to cached playlist at index {CachedOnlineZeeplevels.Count - 1}");

            // Update the current and next playlist indices
            int currentIndex = CurrentPlaylistIndex();
            int nextIndex = NextPlaylistIndex();
            Logger.LogDebug($"PlaylistService: Setting playlist indices - Current: {currentIndex}, Next: {nextIndex}");

            ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex = currentIndex;
            ZeepkistNetwork.CurrentLobby.NextPlaylistIndex = nextIndex;
            ZeepkistNetwork.CurrentLobby.RoundTime = 86400;

            // Update the playlist in the lobby
            Logger.LogDebug($"PlaylistService: Updating lobby playlist with {CachedOnlineZeeplevels.Count} levels");
            ZeepkistNetwork.CurrentLobby.Playlist.Clear();
            ZeepkistNetwork.CurrentLobby.Playlist.AddRange(CachedOnlineZeeplevels);

            // Apply censoring to the last level if needed
            Logger.LogDebug("PlaylistService: Applying censoring to the last level in playlist");
            ZeepkistNetwork.CurrentLobby.Playlist[^1] = GetCensoredLevel(CachedOnlineZeeplevels[^1]);

            // Update the server playlist
            Logger.LogInfo("PlaylistService: Sending updated playlist to server");
            MultiplayerApi.UpdateServerPlaylist();

            Logger.LogInfo($"PlaylistService: Playlist population completed successfully. Playlist now contains {CachedOnlineZeeplevels.Count} levels");
        }
        catch (Exception ex)
        {
            Logger.LogError($"PlaylistService: Error during playlist population: {ex.Message}\nStack trace: {ex.StackTrace}");

            // Try to send a message to players if possible
            try
            {
                ChatMessageService.SendCustomMessage("Error updating playlist. Please check the logs for details.");
            }
            catch
            {
                // Silently ignore if sending message fails
            }
        }
    }

    public async Task ReplaceBrokenLevel()
    {
        try
        {
            Logger.LogInfo("PlaylistService: Attempting to remove last level from playlist");

            if (CachedOnlineZeeplevels == null || CachedOnlineZeeplevels.Count == 0)
            {
                Logger.LogInfo("PlaylistService: Cannot remove level - playlist is empty");
                return;
            }

            CurrentBrokenZeeplevel = ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex];
            Logger.LogDebug($"PlaylistService: Removing level at index {CachedOnlineZeeplevels.Count - 1}");
            CachedOnlineZeeplevels.RemoveAt(CachedOnlineZeeplevels.Count - 1);
            Logger.LogInfo($"PlaylistService: Successfully removed level. Playlist now contains {CachedOnlineZeeplevels.Count} levels");

            await PopulatePlaylist();
        }
        catch (Exception ex)
        {
            Logger.LogError($"PlaylistService: Error removing last level: {ex.Message}");
            throw;
        }
    }


    private OnlineZeeplevel GetCensoredLevel(OnlineZeeplevel onlineZeeplevel)
    {
        OnlineZeeplevel censoredLevel = new OnlineZeeplevel
        {
            UID = onlineZeeplevel.UID,
            WorkshopID = onlineZeeplevel.WorkshopID,
            Name = "???",
            Author = "???",
            played = onlineZeeplevel.played
        };
        return censoredLevel;
    }

    private async Task WaitUntilGameStateNotZero()
    {
        Logger.LogDebug($"PlaylistService: Current GameState is {ZeepkistNetwork.CurrentLobby.GameState}");
        int checkCount = 0;

        do
        {
            checkCount++;
            if (checkCount % 10 == 0) // Log every 10 checks (roughly every 1 second)
            {
                Logger.LogDebug($"PlaylistService: Still waiting for GameState to change from 0 (Current Gamestate: {ZeepkistNetwork.CurrentLobby.GameState}) (waited {checkCount / 10} seconds)");
            }

            await Task.Delay(100); // Check every 100ms to be more responsive
        } while (ZeepkistNetwork.CurrentLobby.GameState != 0);

        Logger.LogInfo($"PlaylistService: GameState changed to {ZeepkistNetwork.CurrentLobby.GameState} after {checkCount} checks");
        await Task.Delay(500); // Additional delay to ensure stability after state change
    }
}