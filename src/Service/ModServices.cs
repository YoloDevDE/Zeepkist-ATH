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
	public LocalLevelCacheService LocalLevelCache { get; } = new();
	public GraphQLService GraphQL { get; } = new();
	public PlaylistService Playlist { get; } = new();

	/// <summary>
	///     Watches the lobby from the moment the mod loads, so a start request can wait for a
	///     race instead of being refused.
	/// </summary>
	public GameStateObserver GameState { get; } = new();

	/// <summary>
	///     Builds the level pool for one run. Lives here because the pool's two sources are
	///     session-scoped even though the pool itself is not.
	/// </summary>
	public RandomLevelService CreateRandomLevelService()
	{
		return new RandomLevelService(GraphQL, LocalLevelCache);
	}
}