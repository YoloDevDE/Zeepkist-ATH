using AuthorTimeHunting.UI;
using UnityEngine;
using ZeepSDK.Messaging;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Sends toast notifications to the top right corner of the game.
///     Call <see cref="Initialize" /> from the plugin before use.
/// </summary>
public static class ToastNotification
{
	// The tag never changes at runtime, so the messenger is created once. Building a
	// TaggedMessenger per call would allocate on per-frame paths.
	private static ITaggedMessenger _tagged;

	public static string Tag => _tagged?.Tag;

	/// <summary>
	///     Creates the tagged messenger every notification is sent through.
	/// </summary>
	/// <param name="tag">Prefix shown on every toast, e.g. "ATH".</param>
	public static void Initialize(string tag)
	{
		_tagged = MessengerApi.CreateTaggedMessenger(tag);
	}

	public static void Info(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Default, duration);
	}

	public static void Success(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Positive, duration);
	}

	public static void Warn(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Negative, duration);
	}

	public static void Error(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Bad, duration);
	}

	/// <summary>An author time claimed - the one toast that gets the mod's own colour.</summary>
	public static void Author(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Author, duration);
	}

	/// <summary>A gold medal claimed.</summary>
	public static void Gold(string message, float duration = 2.5f)
	{
		Tinted(message, HudPalette.Gold, duration);
	}

	/// <summary>
	///     Every toast goes through here rather than through the messenger's own Log/LogWarning
	///     pair. Those exist, but they colour by the game's severity palette, which is the same
	///     near-white for four of the five levels - so a penalty skip and a level loading looked
	///     identical. ATH's palette is the one the rest of the HUD already uses, and the toast is
	///     part of the same reading.
	/// </summary>
	private static void Tinted(string message, Color32 textColor, float duration)
	{
		_tagged?.LogCustomColors(message, textColor, HudPalette.Surface, duration);
	}

	public static void Custom(string message, Color backgroundColor, Color textColor, float duration = 2.5f)
	{
		_tagged?.LogCustomColors(message, textColor, backgroundColor, duration);
	}
}
