using System;
using System.Collections.Generic;

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

	/// <summary>
	///     Read off the player's own config rather than hard-coded, because this is the one mode
	///     whose numbers they can change - a welcome screen promising sixty minutes to somebody
	///     who set it to twenty would be the mod lying about itself on the first screen.
	///     Read once when the welcome screen opens, which is the only place that asks - so this
	///     builds fresh every time and the screen keeps what it got.
	/// </summary>
	public IReadOnlyList<string> Rules
	{
		get
		{
			PluginConfig config = Plugin.Instance.MyConfig;

			return
			[
				$"You get {Minutes(config.Duration.Value)} minutes. When the clock hits zero, the hunt is over.",
				"Random workshop levels, drops included, none of them longer than a three minute author time.",
				"Beat the author time and the next level is drawn. That is the whole loop.",
				"Beat only gold and you may skip on for free. So may the first skip of the run.",
				$"Any skip after that costs {Minutes(config.PenaltyTime.Value)} minutes off the clock.",
				"No level is played twice in the same run."
			];
		}
	}

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

	/// <summary>Seconds as whole minutes, for prose. Rounded up - "0 minutes" is not a rule.</summary>
	private static int Minutes(int seconds)
	{
		return Math.Max(1, (int)Math.Round(seconds / 60.0, MidpointRounding.AwayFromZero));
	}
}
