namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     ATH as it has always been: a fixed time budget, random levels, a penalty for every
///     skip that did not earn a medal, and the run ends when the budget runs out.
///     The only mode that takes its numbers from the player's config. The ones that come
///     after it are defined by their own rules - a Ranked run that could be re-tuned from the
///     settings menu would not be comparable to anyone else's.
/// </summary>
public class ClassicGamemode : IGamemode
{
	public string Id => "classic";

	public string DisplayName => "ATH Solo Hunt (Classic)";

	public string Description => "Collect as many author medals as you can before the clock runs out.";

	public RunSettings CreateSettings()
	{
		PluginConfig config = Plugin.Instance.MyConfig;

		return new RunSettings
		{
			DurationMs = config.Duration.Value * 1000,
			PenaltyTimeMs = config.PenaltyTime.Value * 1000,
			RandomPlaylist = config.RandomPlaylist.Value,
			RejectDuplicateLevels = config.RandomPlaylist.Value
		};
	}
}