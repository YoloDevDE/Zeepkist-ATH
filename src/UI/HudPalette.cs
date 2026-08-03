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

	/// <summary>
	///     The HUD's own backdrop. Translucent rather than opaque: it sits over the track, and
	///     a solid slab at the top of the screen reads as part of the game's UI, not the mod's.
	/// </summary>
	public static readonly Color32 Surface = new(12, 14, 18, 214);

	/// <summary>The unfilled part of the time-budget bar.</summary>
	public static readonly Color32 Track = new(255, 255, 255, 38);

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

	/// <summary>
	///     What each control button is tinted with. Colour rather than position is what makes a
	///     button findable mid-run: the panel is glanced at with a kart in the air, and by then
	///     "the red one" has been read and "Stop Hunt" has not.
	/// </summary>
	public static readonly Color32 ActionSkip = new(46, 104, 168, 255);

	public static readonly Color32 ActionBroken = new(168, 106, 34, 255);
	public static readonly Color32 ActionPause = new(140, 118, 26, 255);
	public static readonly Color32 ActionResume = new(46, 132, 60, 255);
	public static readonly Color32 ActionRestart = new(78, 78, 122, 255);
	public static readonly Color32 ActionStop = new(150, 46, 46, 255);

	/// <summary>
	///     The running time's warning ladder, walked as a medal is closed in on and then lost.
	///     One colour per tier rather than a gradient: the number is read at a glance while
	///     driving, and a smooth fade tells you nothing at a glance - a colour that has clearly
	///     changed does. Five tiers is as many as can still be told apart in peripheral vision.
	/// </summary>
	public static readonly Color32 PaceSafe = new(255, 255, 255, 255);

	/// <summary>The medal is still ahead, but not by much.</summary>
	public static readonly Color32 PaceClose = new(255, 226, 84, 255);

	/// <summary>The author time is gone. The next medal down is still comfortably ahead.</summary>
	public static readonly Color32 PaceLost = new(255, 146, 48, 255);

	/// <summary>The last medal left is about to go too.</summary>
	public static readonly Color32 PaceCritical = new(255, 74, 74, 255);

	/// <summary>Nothing left to chase on this attempt.</summary>
	public static readonly Color32 PaceGone = new(150, 40, 40, 255);

	public static readonly Color32 Command = new(122, 255, 122, 255);
	public static readonly Color32 Info = new(170, 170, 170, 255);
	public static readonly Color32 Alert = new(255, 0, 0, 255);
}
