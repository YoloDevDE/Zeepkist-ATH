using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using ZeepkistClient;
using ZeepkistNetworking;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting.Service;

public class RandomLevelService
{
    public static LevelItem PrevLevel { get; set; }
    public static LevelItem CurrentLevel { get; set; }
    public static LevelItem NextLevel { get; set; }

    public static async void GenerateRandomLevel()
    {
        List<LevelItem> randomLevelAsync = await GraphQLService.Instance.GetRandomLevelAsync();
        PrevLevel = CurrentLevel;
        CurrentLevel = NextLevel ?? randomLevelAsync[0];
        NextLevel = randomLevelAsync[1];
    }

    public static void RemoveCurrentLevelFromPlaylist()
    {
        int currentIndex = ZeepkistNetwork.CurrentLobby.CurrentPlaylistIndex;
        ZeepkistNetwork.CurrentLobby.Playlist.RemoveAt(currentIndex);

        if (currentIndex < ZeepkistNetwork.CurrentLobby.Playlist.Count)
        {
            MultiplayerApi.SetNextLevelIndex(currentIndex);
        }
        else if (ZeepkistNetwork.CurrentLobby.Playlist.Count > 0)
        {
            MultiplayerApi.SetNextLevelIndex(ZeepkistNetwork.CurrentLobby.Playlist.Count - 1);
        }
    }

    public static void NextLevelProcedure()
    {
        LevelItem currentLevelItem = CurrentLevel;
        LevelItem nextLevelItem = NextLevel;

        PlaylistItem currentLevelPlaylistItem = new PlaylistItem(
            currentLevelItem.FileUid,
            currentLevelItem.WorkshopId,
            currentLevelItem.Name,
            currentLevelItem.FileAuthor
        );
        PlaylistItem nextLevelPlaylistItem = new PlaylistItem(
            nextLevelItem.FileUid,
            nextLevelItem.WorkshopId,
            "???",
            "???"
        );
        if (ZeepkistNetwork.CurrentLobby.Playlist.Count > 0)
        {
            ZeepkistNetwork.CurrentLobby.Playlist[^1] = new OnlineZeeplevel
            {
                Author = 
            }
        }

        ZeepkistNetwork.CurrentLobby.RoundTime = 86400;
        MultiplayerApi.AddLevelToPlaylist(currentLevelPlaylistItem, false);
        MultiplayerApi.AddLevelToPlaylist(nextLevelPlaylistItem, true);
        MultiplayerApi.UpdateServerPlaylist();
        GenerateRandomLevel();
    }
}