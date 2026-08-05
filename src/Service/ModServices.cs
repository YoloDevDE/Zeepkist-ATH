using AuthorTimeHunting.Gamemodes;
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
		Toolbar = new AthToolbar(this);
	}

	/// <summary>
	///     ATH's menu in the game's top bar. Holds a reference back to the services because it
	///     is a view over all of them rather than a thing of its own.
	/// </summary>
	public AthToolbar Toolbar { get; }

	/// <summary>
	///     The modes ATH can be played in, and which one the next run will use. Session-scoped
	///     because the choice outlives a run - stopping a hunt should not forget what was picked.
	/// </summary>
	public GamemodeRegistry Gamemodes { get; } = new();

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
	///     The mod's main panel: the clock, the score and the controls. Session-scoped because
	///     it is also what a player sees when no run is going - that is where Start lives.
	/// </summary>
	public ControlPanel Control { get; } = new();

	/// <summary>
	///     What the level being played costs and how it compares. Shown alongside the control
	///     panel and toggled with it - the two are one UI, drawn as two windows.
	/// </summary>
	public LevelStatsPanel LevelStats { get; } = new();

	/// <summary>
	///     The run's clock, score and budget, across the top of the screen. Not part of the
	///     control panel: it is read constantly and clicked never, which is the opposite of
	///     everything the panel holds.
	/// </summary>
	public RunOverlay RunOverlay { get; } = new();

	/// <summary>
	///     The developer panel. Off unless the config switch is on - it can hand out medals and
	///     redraw the level pool, which is not something a hunt should be able to do by accident.
	/// </summary>
	public DebugPanel Debug { get; } = new();

	/// <summary>
	///     The card between two levels: what the last one came to, and the run so far. Shows
	///     itself when a level ends and takes itself down when the next one starts.
	/// </summary>
	public LevelSummaryOverlay LevelSummary { get; } = new();

	/// <summary>
	///     Rewrites the game's own running-time label while a hunt is on. Not a drawer - it
	///     writes into the game's UI rather than ours, so it is not registered with UIApi.
	/// </summary>
	public RaceTimeDisplay RaceTime { get; } = new();

	/// <summary>
	///     The board with the author and gold times standing in it. Its own window rather than a
	///     rewrite of the game's own rows, so it can be moved and later put over them.
	/// </summary>
	public LeaderboardOverlay Leaderboard { get; } = new();

	/// <summary>
	///     The end-of-run report. Session-scoped and holding a snapshot, because the run it
	///     reports on is torn down the moment it stops.
	/// </summary>
	public ResultsScreen Results { get; } = new();

	/// <summary>
	///     The screen that explains the mod, up on startup unless it has been switched off.
	///     Session-scoped like every other window, and deliberately not tied to a run: it is read
	///     before the first one and reopened between them.
	/// </summary>
	public WelcomeWindow Welcome { get; } = new();

	/// <summary>
	///     Where the windows are and what can be typed. The reference half of the welcome screen,
	///     split off because it is the half that gets reopened.
	/// </summary>
	public HelpWindow Help { get; } = new();

	/// <summary>
	///     Every run that ever finished, kept on disk. Session-scoped so the file is read once
	///     rather than on every results screen.
	/// </summary>
	public MatchHistoryService History { get; } = new();

	/// <summary>
	///     Tells the UI which run is in progress, or null when none is. One call rather than
	///     two assignments, so a new drawer cannot be left reading a stale run.
	/// </summary>
	public void PublishRun(AthStateMachine run)
	{
		// The frame cache is static and would otherwise keep the run that just ended reachable
		// for the rest of the session. This is the moment the run changed, so this is where it
		// is let go of.
		RunHudView.Clear();

		Control.ActiveRun = run;
		LevelStats.ActiveRun = run;
		LevelStats.Visible = run != null;
		RunOverlay.ActiveRun = run;
		RunOverlay.Visible = run != null;
		Debug.ActiveRun = run;
		RaceTime.ActiveRun = run;
		Leaderboard.ActiveRun = run;
	}

	/// <summary>What /ath does: shows or hides the mod, all of it together.</summary>
	public void ToggleUi()
	{
		Control.Toggle();
		LevelStats.Visible = Control.Visible && LevelStats.ActiveRun != null;
		RunOverlay.Visible = Control.Visible && RunOverlay.ActiveRun != null;

		// Opening the mod is the moment the welcome screen is for, and it is the only moment:
		// nobody types /ath to be told the rules of a run they are already in, so a hunt in
		// progress keeps it away. It goes down again with everything else.
		Welcome.Visible = Control.Visible && Plugin.Instance.MyConfig.ShowWelcome.Value && Control.ActiveRun == null;
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
