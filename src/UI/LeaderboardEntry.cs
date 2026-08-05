using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>One line of the merged board. A medal is an entry like any other - that is the point.</summary>
public readonly struct LeaderboardEntry
{
	public LeaderboardEntry(string name, float time, Color32 colour, bool isLocal, string text)
	{
		Name = name;
		Time = time;
		Colour = colour;
		IsLocal = isLocal;
		Text = text;
	}

	public string Name { get; }

	/// <summary>What the row sorts by. Not what it displays - see <see cref="Text" />.</summary>
	public float Time { get; }

	public Color32 Colour { get; }
	public bool IsLocal { get; }

	/// <summary>
	///     What the right-hand column reads, formatted once when the board is rebuilt rather
	///     than on every frame it is drawn.
	/// </summary>
	public string Text { get; }
}
