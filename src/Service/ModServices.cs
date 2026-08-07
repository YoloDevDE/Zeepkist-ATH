using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Screens;
using AuthorTimeHunting.UI.Views;

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
		DebugToolbar = new AthDebugToolbar(this);
		Health = new HealthService(GraphQL);
	}

	public AthToolbar Toolbar { get; }

	public AthDebugToolbar DebugToolbar { get; }

	public GamemodeRegistry Gamemodes { get; } = new();

	public LocalLevelCacheService LocalLevelCache { get; } = new();
	public GraphQLService GraphQL { get; } = new();

	public HealthService Health { get; }

	public WorkshopDownloadService WorkshopDownloads { get; } = new();

	public PlaylistService Playlist { get; }

	public GameStateObserver GameState { get; } = new();

	public TraceService Trace { get; } = new();

	public AthMenu Menu { get; } = new();

	public StatusWindow Status { get; } = new();

	public ControlPanel Control { get; } = new();

	public LevelStatsPanel LevelStats { get; } = new();

	public RunOverlay RunOverlay { get; } = new();

	public DebugPanel Debug { get; } = new();

	public LevelSummaryOverlay LevelSummary { get; } = new();

	public RaceTimeDisplay RaceTime { get; } = new();

	public LeaderboardOverlay Leaderboard { get; } = new();

	public ResultsScreen Results { get; } = new();

	public WelcomeWindow Welcome { get; } = new();

	public HelpWindow Help { get; } = new();

	public LoadingOverlay Loading { get; } = new();

	public LobbySilence Silence { get; } = new();

	public PlayMenuButton PlayMenu { get; } = new();

	public MatchHistoryService History { get; } = new();

	public void PublishRun(AthStateMachine run)
	{
		RunHudView.Clear();

		Control.ActiveRun = run;
		LevelStats.ActiveRun = run;
		LevelStats.Visible = run != null;
		RunOverlay.ActiveRun = run;
		RunOverlay.Visible = run != null;
		Debug.ActiveRun = run;
		Status.ActiveRun = run;
		RaceTime.ActiveRun = run;
		Leaderboard.ActiveRun = run;
		Loading.ActiveRun = run;
	}

	/// <summary>
	///     What the menu's Quit does: every window ATH owns goes away, the run itself is left
	///     alone. A player who wants the screen back has /ath, and one who wants the hunt over
	///     has the Stop button.
	/// </summary>
	public void HideUi()
	{
		Menu.Visible = false;
		Status.Visible = false;
		Control.Visible = false;
		LevelStats.Visible = false;
		RunOverlay.Visible = false;
		Leaderboard.Visible = false;
		Welcome.Visible = false;
		Help.Visible = false;
		Debug.Visible = false;
		Results.Close();
	}

	public RandomLevelService CreateRandomLevelService()
	{
		return new RandomLevelService(GraphQL, LocalLevelCache);
	}
}
