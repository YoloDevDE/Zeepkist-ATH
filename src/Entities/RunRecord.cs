using System;

namespace AuthorTimeHunting.Entities;

/// <summary>
///     One finished run, as it is written to disk.
///     Flat and primitive on purpose: this file outlives the mod version that wrote it, and a
///     record that referenced <see cref="Level" /> would break the moment that class gained a
///     field. Everything here is already formatted-ready and nothing is derived, so an old
///     record stays readable no matter what the rest of the mod does afterwards.
/// </summary>
public class RunRecord
{
	/// <summary>When the run ended, local time. Sorting key for the whole history.</summary>
	public DateTime EndedAt { get; set; }

	/// <summary>Who was playing. Runs are per-machine, but not necessarily per-account.</summary>
	public string PlayerName { get; set; }

	public string Gamemode { get; set; }

	public int LevelsPlayed { get; set; }
	public int AuthorMedals { get; set; }
	public int GoldMedals { get; set; }
	public int Penalties { get; set; }
	public int TotalAttempts { get; set; }

	/// <summary>The budget the run was given, in milliseconds.</summary>
	public double DurationMs { get; set; }

	/// <summary>How much of the budget was actually spent driving, in milliseconds.</summary>
	public double DrivenMs { get; set; }

	/// <summary>True when the clock ran out rather than the player stopping early.</summary>
	public bool RanOutOfTime { get; set; }
}
