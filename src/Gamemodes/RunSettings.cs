namespace AuthorTimeHunting.Gamemodes;

/// <summary>
///     The rules of one run, fixed the moment it starts.
///     This is what separates one gamemode from another. Everything here used to be read
///     from <see cref="PluginConfig" /> at the point of use, which meant the rules were
///     scattered across five states and there was exactly one possible set of them - the
///     player's. A mode hands over its own set instead, and the states stop caring where
///     the numbers came from.
///     Deliberately a plain settings object rather than a set of behaviour hooks. The modes
///     on the roadmap that only change numbers (Custom, Ranked, Duel) need nothing more, and
///     the ones that change rules outright (Survival's clock, WR hunt's targets) will want
///     hooks shaped by what they actually do, not by what we guessed today.
/// </summary>
public class RunSettings
{
	public int DurationMs { get; set; }

	public int PenaltyTimeMs { get; set; }

	public bool RandomPlaylist { get; set; } = true;

	public int FreeSkips { get; set; } = 1;

	public bool RejectDuplicateLevels { get; set; } = true;
}
