using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     The level the lobby is about to load, as much of it as is known before it loads.
///     The playlist the host pushed carries a uid, a name and an author and nothing else, so the
///     two times come from the pool the run drew the level from - which has them for everything it
///     fetched from the API, and has nothing at all for a lobby playing its own playlist. A time
///     nobody knows yet is a dash: the level is a few seconds away and will say so itself.
/// </summary>
public class NextLevelView
{
	private const string _unknown = "-";

	public string LevelUid { get; private set; }

	public string Name { get; private set; }

	public string Author { get; private set; }

	public string AuthorTime { get; private set; }

	public string GoldTime { get; private set; }

	public static NextLevelView From(AthStateMachine run)
	{
		OnlineZeeplevel queued = Queued();

		if (queued == null || string.IsNullOrEmpty(queued.UID))
		{
			return null;
		}

		LevelItem known = run?.RandomLevels.Drawn(queued.UID);

		return new NextLevelView
		{
			LevelUid = queued.UID,
			Name = queued.Name,
			Author = queued.Author,
			AuthorTime = Formatted(known?.ValidationTimeAuthor),
			GoldTime = Formatted(known?.ValidationTimeGold)
		};
	}

	private static OnlineZeeplevel Queued()
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

	private static string Formatted(float? seconds)
	{
		if (seconds == null || seconds <= 0f)
		{
			return _unknown;
		}

		return TimeFormatter.FormatTime(seconds.Value);
	}
}
