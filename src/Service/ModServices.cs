using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;

namespace AuthorTimeHunting.Service;

/// <summary>
///     The services that live as long as the game session does, constructed once in
///     Plugin.Awake and handed down from there. This is the mod's composition root: the one
///     place that decides what exists and what depends on what.
///     Everything here is deliberately session-scoped:
///     <list type="bullet">
///         <item><see cref="LocalLevelCache" /> caches what is on disk.</item>
///         <item><see cref="GraphQL" /> owns an HTTP client.</item>
///         <item>
///             <see cref="Playlist" /> enforces the server's 5 second push limit, which is
///             global and does not reset between runs.
///         </item>
///     </list>
///     Run-scoped state does NOT belong here. RandomLevelService is the counter-example: its
///     level pool belongs to a single run and is therefore created by AthStateMachine.
/// </summary>
public class ModServices
{
	public ModServices()
	{
		Playlist = new PlaylistService(WorkshopDownloads);
	}

	public LocalLevelCacheService LocalLevelCache { get; } = new();
	public GraphQLService GraphQL { get; } = new();

	/// <summary>Pre-fetches workshop levels so the podium does not wait for Steam.</summary>
	public WorkshopDownloadService WorkshopDownloads { get; } = new();

	public PlaylistService Playlist { get; }

	/// <summary>
	///     Watches the lobby from the moment the mod loads, so a start request can wait for a
	///     race instead of being refused.
	/// </summary>
	public GameStateObserver GameState { get; } = new();

	/// <summary>
	///     The always-on run display. Session-scoped like the rest of the UI; it shows itself
	///     when <see cref="PublishRun" /> hands it a run.
	/// </summary>
	public AthHud Hud { get; } = new();

	/// <summary>
	///     The control panel behind /ath. Session-scoped because it is also what a player sees
	///     when no run is going - that is where the Start button lives.
	/// </summary>
	public AthWindow Window { get; } = new();

	/// <summary>Centre-screen banners and notifications. Session-scoped like the window.</summary>
	public AthOverlay Overlay { get; } = new();

	/// <summary>
	///     Tells the UI which run is in progress, or null when none is. One call rather than
	///     two assignments, so a new drawer cannot be left reading a stale run.
	/// </summary>
	public void PublishRun(AthStateMachine run)
	{
		Hud.ActiveRun = run;
		Window.ActiveRun = run;
	}

	/// <summary>
	///     Builds the level pool for one run. Lives here because the pool's two sources are
	///     session-scoped even though the pool itself is not.
	/// </summary>
	public RandomLevelService CreateRandomLevelService()
	{
		return new RandomLevelService(GraphQL, LocalLevelCache);
	}
}