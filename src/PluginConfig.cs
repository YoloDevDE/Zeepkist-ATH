using BepInEx.Configuration;

namespace AuthorTimeHunting;

/// <summary>
///     Handles configuration settings for the plugin
/// </summary>
public class PluginConfig
{
	public PluginConfig(ConfigFile config)
	{
		RandomPlaylist = config.Bind("Gameplay", "RTM", true,
			"If enabled, the levels will be random. If disabled, the current playlist will be used.");

		Duration = config.Bind("Gameplay", "Duration", 3600,
			"Total run duration in seconds. Default is 3600 (60 minutes).");

		PenaltyTime = config.Bind("Gameplay", "Penalty Time", 300,
			"Penalty time per failed level in seconds. Default is 300 (5 minutes).");

		Minimalist = config.Bind("Misc", "Minimalist", false, "Makes it a bit less text");

		InGameHud = config.Bind("Misc", "In-Game HUD", true,
			"Draws the run HUD as a movable in-game window instead of the server message block.");

		SavePlaylistOnRunEnd = config.Bind("Misc", "Save Playlist on Run End", false,
			"Literally what it says. what did you expect");

		RaceTimeColorChange = config.Bind("Race Timer", "Colour the Run Time", true,
			"Tints the running time by the medal it is currently on: author, gold, or neither.");

		// Off by default since the medals moved into the leaderboard. Both at once put the
		// author and gold times on screen three times over - board, timer, level panel.
		RaceTimeShowTarget = config.Bind("Race Timer", "Show Next Medal", false,
			"Adds a line under the running time naming the best medal still within reach and the time it needs. "
			+ "Redundant while the leaderboard carries the medals.");

		LeaderboardMedals = config.Bind("Race Timer", "Medals in the Leaderboard", true,
			"Adds the author and gold times to the small in-race leaderboard as if they were two more players, "
			+ "with the gap to your own time instead of theirs.");

		GraphQlUrl = config.Bind("Backend", "GraphQL URL", "https://graphql.zeepki.st/",
			"GraphQL endpoint used for level queries.");
	}

	/// <summary>
	///     When enabled, the playlist will be saved when a run ends
	/// </summary>
	public ConfigEntry<bool> SavePlaylistOnRunEnd { get; }

	/// <summary>
	///     When enabled, shows less text in the UI
	/// </summary>
	public ConfigEntry<bool> Minimalist { get; }

	/// <summary>
	///     When enabled, the run HUD is drawn as an in-game window instead of being written
	///     into the game's server message area
	/// </summary>
	public ConfigEntry<bool> InGameHud { get; }

	/// <summary>
	///     When enabled, the playlist will be random
	/// </summary>
	public ConfigEntry<bool> RandomPlaylist { get; }

	/// <summary>
	///     Total run duration in seconds
	/// </summary>
	public ConfigEntry<int> Duration { get; }

	/// <summary>
	///     Penalty time per failed level in seconds
	/// </summary>
	public ConfigEntry<int> PenaltyTime { get; }

	/// <summary>
	///     GraphQL endpoint URL
	/// </summary>
	public ConfigEntry<string> GraphQlUrl { get; }

	#region Race Timer

	// The three switches over the game's own running-time display. Kept separate rather
	// than one "style" enum because they are genuinely independent: colour without deltas
	// is a perfectly reasonable setup, and so is the reverse.

	/// <summary>Tint the running time by the medal it currently sits on.</summary>
	public ConfigEntry<bool> RaceTimeColorChange { get; }

	/// <summary>Show the medal still in reach, and its time, under the running time.</summary>
	public ConfigEntry<bool> RaceTimeShowTarget { get; }

	/// <summary>Sort the author and gold times into the small in-race leaderboard.</summary>
	public ConfigEntry<bool> LeaderboardMedals { get; }

	#endregion
}
