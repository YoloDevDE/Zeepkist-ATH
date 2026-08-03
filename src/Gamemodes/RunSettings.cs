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
	/// <summary>Time budget for the whole run, in milliseconds.</summary>
	public int DurationMs { get; set; }

	/// <summary>What a penalty skip costs, in milliseconds.</summary>
	public int PenaltyTimeMs { get; set; }

	/// <summary>
	///     Draw random levels as the run goes, rather than playing the lobby's own playlist
	///     from front to back. The lobby-playlist mode is how a curated run is played.
	/// </summary>
	public bool RandomPlaylist { get; set; } = true;

	/// <summary>Skips the run grants for free before penalties start applying.</summary>
	public int FreeSkips { get; set; } = 1;

	/// <summary>
	///     Send a level that has already been played this run back and draw another. Only
	///     meaningful with <see cref="RandomPlaylist" /> - a curated playlist is allowed to
	///     repeat a level on purpose.
	/// </summary>
	public bool RejectDuplicateLevels { get; set; } = true;
}
