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

		SavePlaylistOnRunEnd = config.Bind("Misc", "Save Playlist on Run End", false,
			"Literally what it says. what did you expect");

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
}