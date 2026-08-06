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
	public DateTime EndedAt { get; set; }

	public string PlayerName { get; set; }

	public string Gamemode { get; set; }

	public int LevelsPlayed { get; set; }
	public int AuthorMedals { get; set; }
	public int GoldMedals { get; set; }
	public int Penalties { get; set; }
	public int TotalAttempts { get; set; }

	public double DurationMs { get; set; }

	public double DrivenMs { get; set; }

	public bool RanOutOfTime { get; set; }

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

	public string Status { get; set; }

	public int Attempts { get; set; }
	public int Crashes { get; set; }
	public int WheelsLost { get; set; }

	public double DurationMs { get; set; }

	public double AuthorTime { get; set; }
	public double GoldTime { get; set; }

	public double PersonalBest { get; set; }
}
