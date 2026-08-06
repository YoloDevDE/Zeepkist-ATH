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

		StartLights = config.Bind("Race Timer", "Start Lights", true,
			"Replaces the running time with three lights during the start countdown.");

		LeaderboardMedals = config.Bind("Race Timer", "Medals in the Leaderboard", true,
			"Adds the author and gold times to the small in-race leaderboard as if they were two more players, "
			+ "with the gap to your own time instead of theirs.");

		GraphQlUrl = config.Bind("Backend", "GraphQL URL", "https://graphql.zeepki.st/",
			"GraphQL endpoint used for level queries.");

		GtrUrl = config.Bind("Backend", "GTR URL", "https://backend.zeepki.st/",
			"GTR backend, checked by the status window. ATH does not call it - it is here because a hunt is "
			+ "worth little if the times are not being recorded.");
	}

	public ConfigEntry<bool> SavePlaylistOnRunEnd { get; }

	public ConfigEntry<bool> Minimalist { get; }

	public ConfigEntry<bool> InGameHud { get; }

	public ConfigEntry<bool> RandomPlaylist { get; }

	public ConfigEntry<int> Duration { get; }

	public ConfigEntry<int> PenaltyTime { get; }

	public ConfigEntry<string> GraphQlUrl { get; }

	public ConfigEntry<string> GtrUrl { get; }

	#region Race Timer

	public ConfigEntry<bool> StartLights { get; }

	public ConfigEntry<bool> LeaderboardMedals { get; }

	#endregion
}
