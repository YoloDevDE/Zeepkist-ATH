using BepInEx.Configuration;

namespace AuthorTimeHunting;

/// <summary>
///     Handles configuration settings for the plugin
/// </summary>
public class PluginConfig
{
    public PluginConfig(ConfigFile config)
    {
        SavePlaylistOnRunEnd = config.Bind(
            "General",
            "Save Playlist on Run End",
            false,
            "Literally what it says. what did you expect"
        );

        Minimalist = config.Bind(
            "General",
            "Minimalist",
            false,
            "Makes it a bit less text"
        );
    }

    /// <summary>
    ///     When enabled, the playlist will be saved when a run ends
    /// </summary>
    public ConfigEntry<bool> SavePlaylistOnRunEnd { get; }

    /// <summary>
    ///     When enabled, shows less text in the UI
    /// </summary>
    public ConfigEntry<bool> Minimalist { get; }
}