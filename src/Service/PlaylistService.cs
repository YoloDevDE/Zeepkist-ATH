using System.Threading.Tasks;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.Service;

public class PlaylistService
{
    private static PlaylistService _instance;

    private PlaylistService()
    {
        ZeepkistNetwork.LobbyPlaylistChanged += OnPlaylistChanged;
    }

    public static PlaylistService Instance => _instance ??= new PlaylistService();

    public bool IsPlaylistReady { get; set; } = true;

    public async Task WaitForPlaylistReady()
    {
        while (!Instance.IsPlaylistReady)
        {
            await Task.Delay(100); // Check every 100ms to not block the thread
        }
    }

    public OnlineZeeplevel GetNextLevel()
    {
        return ZeepkistNetwork.CurrentLobby.Playlist[ZeepkistNetwork.CurrentLobby.NextPlaylistIndex];
    }

    private async void OnPlaylistChanged()
    {
        IsPlaylistReady = false;

        await Task.Delay(3000);

        IsPlaylistReady = true;
    }

    public void Dispose()
    {
        ZeepkistNetwork.LobbyPlaylistChanged -= OnPlaylistChanged;
        _instance = null;
    }
}