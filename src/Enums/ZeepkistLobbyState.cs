namespace AuthorTimeHunting.Enums;

/// <summary>
///     The lobby's game state. The game stores this as a bare int on ZeepkistLobby.GameState
///     and has no enum for it; these values were read off the decompiled client:
///     <list type="bullet">
///         <item>0 - PhotonZeepkist closes all UI and race results are accepted.</item>
///         <item>1 - PhotonZeepkist calls DoShowBuffer(), OnlineGameplayUI fades RoundOverText in.</item>
///         <item>2 - PhotonZeepkist calls DoShowWinscreen(), the podium bar counts down 10s.</item>
///     </list>
///     There is no value beyond 2 anywhere in the client.
///     CAREFUL: 0 is also the default value of an uninitialized int, so a lobby that has not
///     received a ChangeLobbyGameStatePacket yet reads as <see cref="Racing" />. Never treat
///     this value on its own as "the race is running" - use GameStateObserver.IsRacing, which
///     pairs it with the level actually being loaded.
/// </summary>
public enum ZeepkistLobbyState
{
	/// <summary>The race is on. Also the value of a lobby that has not reported yet.</summary>
	Racing = 0,

	/// <summary>The round is over, the game shows the end-of-round buffer screen.</summary>
	Ending = 1,

	/// <summary>The podium is showing, the next level is about to load.</summary>
	Podium = 2
}