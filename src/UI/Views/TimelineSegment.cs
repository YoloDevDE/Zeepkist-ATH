using UnityEngine;

namespace AuthorTimeHunting.UI.Views;

/// <summary>
///     One stretch of the hour, and what it was spent on.
///     The run bar used to be a single fill: one number, the fraction of the budget still
///     unspent. That says how much is left and nothing about how it went, which is the more
///     interesting half - a hunt with six author medals and one that lost four levels to
///     penalties can stand at the same fraction and are not the same run.
/// </summary>
public class TimelineSegment
{
	public TimelineSegment(float fraction, Color32 colour)
	{
		Fraction = fraction;
		Colour = colour;
	}

	/// <summary>How much of the whole budget this stretch took, between 0 and 1.</summary>
	public float Fraction { get; }

	public Color32 Colour { get; }
}
