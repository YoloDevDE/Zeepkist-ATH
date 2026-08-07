using AuthorTimeHunting.Entities;

namespace AuthorTimeHunting.UI.Views;

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

	public string Medals { get; }

	public RunRecord Record { get; }

	public string When { get; }
	public string Gamemode { get; }
	public string Levels { get; }
	public string Driven { get; }

	public bool IsCurrent { get; }
}
