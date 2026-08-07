using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

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

	public float Time { get; }

	public Color32 Colour { get; }
	public bool IsLocal { get; }

	public string Text { get; }
}
