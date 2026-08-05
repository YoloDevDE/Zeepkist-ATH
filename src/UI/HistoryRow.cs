using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.UI;

/// <summary>One past run, as the five columns the history tab shows.</summary>
public readonly struct HistoryRow
{
	public HistoryRow(RunRecord record, string when, string gamemode, int authorMedals, int goldMedals,
		int penalties, string levels, string driven, bool isCurrent)
	{
		Record = record;
		When = when;
		Gamemode = gamemode;
		Levels = levels;
		Driven = driven;
		IsCurrent = isCurrent;
		Medals = $"{authorMedals} / {goldMedals} / {penalties}";
	}

	/// <summary>The three counts as one column, composed once rather than on every frame.</summary>
	public string Medals { get; }

	/// <summary>
	///     What was stored, kept alongside the formatted columns so clicking the row can open
	///     the whole run rather than the five things this line happens to show.
	/// </summary>
	public RunRecord Record { get; }

	public string When { get; }
	public string Gamemode { get; }
	public string Levels { get; }
	public string Driven { get; }

	/// <summary>True for the run that was just played.</summary>
	public bool IsCurrent { get; }
}
