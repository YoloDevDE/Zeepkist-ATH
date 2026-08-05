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

	/// <summary>
	///     Name and author as the list draws them. Composed here rather than at the draw,
	///     because the list is redrawn every frame the report is up and neither half of this
	///     can change while it is.
	/// </summary>
	public string Title { get; }

	/// <summary>
	///     The byline, the shouted status, and the best time with its delta - all as the
	///     detail view draws them. Same reason as <see cref="Title" />: a detail view is on
	///     screen for as long as it is being read, and none of this changes while it is.
	/// </summary>
	public string ByAuthor { get; }

	public string StatusUpper { get; }

	/// <summary>Null when the level was never finished, which is also how the view asks.</summary>
	public string BestWithDelta { get; }

	/// <summary>The level's own id, which is what a thumbnail is looked up by.</summary>
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

	/// <summary>
	///     The same row under a different number. For a list that shows only the tail of a
	///     run and still has to say which levels these actually were.
	/// </summary>
	public LevelRow Renumbered(int index)
	{
		Index = index;

		return this;
	}
}
