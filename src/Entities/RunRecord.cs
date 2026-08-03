using System;
using System.Collections.Generic;

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

	/// <summary>
	///     Every level the run touched, in the order they were played. Null on records written
	///     before runs kept their levels - the history opens those as a summary and says so
	///     rather than pretending the run played nothing.
	/// </summary>
	public List<RunLevelRecord> Levels { get; set; }
}

/// <summary>
///     One level inside a stored run. Flat and primitive for the same reason as
///     <see cref="RunRecord" />: it has to stay readable long after the code that wrote it.
/// </summary>
public class RunLevelRecord
{
	public string Uid { get; set; }
	public string Name { get; set; }
	public string Author { get; set; }

	/// <summary>What the level ended as, in the words the report uses.</summary>
	public string Status { get; set; }

	public int Attempts { get; set; }
	public int Crashes { get; set; }
	public int WheelsLost { get; set; }

	public double DurationMs { get; set; }

	public double AuthorTime { get; set; }
	public double GoldTime { get; set; }

	/// <summary>The best time driven here, or negative when the level was never finished.</summary>
	public double PersonalBest { get; set; }
}
