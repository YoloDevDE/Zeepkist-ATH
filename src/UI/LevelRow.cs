using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     One level of a run: the five columns the list shows, and everything behind them that
///     the detail view opens up.
///     A class rather than a struct because it doubles as "which level is selected", and a
///     null reference is a cleaner way to say "none" than an index that has to be kept in
///     step with a list that is rebuilt every frame.
/// </summary>
public class LevelRow
{
	public LevelRow(int index, string uid, string name, string author, string status, Color32 statusColour,
		string attempts, string duration, string authorTime, string goldTime, string personalBest,
		string authorDelta, string crashes, string wheelsLost)
	{
		Index = index;
		Uid = uid;
		Name = name;
		Status = status;
		StatusColour = statusColour;
		Attempts = attempts;
		Duration = duration;
		AuthorTime = authorTime;
		GoldTime = goldTime;
		Crashes = crashes;
		WheelsLost = wheelsLost;
		Title = $"{name}  ({author})";
		ByAuthor = $"by {author}";
		StatusUpper = status?.ToUpperInvariant();
		BestWithDelta = personalBest == null ? null : $"{personalBest}   ({authorDelta})";
	}

	public int Index { get; private set; }

	public string Title { get; }

	public string ByAuthor { get; }

	public string StatusUpper { get; }

	public string BestWithDelta { get; }

	public string Uid { get; }

	public string Name { get; }
	public string Status { get; }
	public Color32 StatusColour { get; }
	public string Attempts { get; }
	public string Duration { get; }

	public string AuthorTime { get; }
	public string GoldTime { get; }

	public string Crashes { get; }
	public string WheelsLost { get; }

	public LevelRow Renumbered(int index)
	{
		Index = index;

		return this;
	}
}
