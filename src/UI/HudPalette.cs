using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The HUD's colours in one place.
///     They used to be roughly sixty hex string literals scattered through AthStateMachine
///     and AthCtx (finding M4). Anything the in-game UI draws picks its colour from here; the
///     old server message still carries its own literals until it is retired.
/// </summary>
public static class HudPalette
{
	public static readonly Color32 Default = new(230, 230, 230, 255);
	public static readonly Color32 Muted = new(153, 153, 153, 255);
	public static readonly Color32 Section = new(255, 212, 166, 255);

	public static readonly Color32 Good = new(66, 179, 54, 255);
	public static readonly Color32 Warning = new(179, 179, 0, 255);
	public static readonly Color32 Danger = new(191, 57, 57, 255);

	public static readonly Color32 Author = new(230, 0, 230, 255);
	public static readonly Color32 Gold = new(255, 214, 0, 255);
	public static readonly Color32 FreeSkip = new(0, 255, 255, 255);
	public static readonly Color32 Penalty = new(191, 57, 57, 255);

	/// <summary>The skip that ends the run - deliberately near-black, it should look wrong.</summary>
	public static readonly Color32 Fatal = new(15, 15, 15, 255);

	public static readonly Color32 White = new(255, 255, 255, 255);

	/// <summary>Section headings inside a panel.</summary>
	public static readonly Color32 Heading = new(179, 54, 163, 255);

	/// <summary>Labels on the left of a row.</summary>
	public static readonly Color32 Key = new(127, 219, 255, 255);

	public static readonly Color32 LevelName = new(100, 210, 255, 255);
	public static readonly Color32 AuthorName = new(255, 215, 0, 255);

	/// <summary>A time beaten, a medal claimed - anything that went well.</summary>
	public static readonly Color32 Positive = new(80, 228, 81, 255);

	/// <summary>A time missed.</summary>
	public static readonly Color32 Negative = new(237, 178, 39, 255);

	/// <summary>Time lost, penalties paid.</summary>
	public static readonly Color32 Bad = new(255, 90, 90, 255);

	public static readonly Color32 Command = new(122, 255, 122, 255);
	public static readonly Color32 Info = new(170, 170, 170, 255);
	public static readonly Color32 Alert = new(255, 0, 0, 255);
}