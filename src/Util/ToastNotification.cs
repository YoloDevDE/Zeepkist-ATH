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
		_tagged?.Log(message, duration);
	}

	public static void Success(string message, float duration = 2.5f)
	{
		_tagged?.LogSuccess(message, duration);
	}

	public static void Warn(string message, float duration = 2.5f)
	{
		_tagged?.LogWarning(message, duration);
	}

	public static void Error(string message, float duration = 2.5f)
	{
		_tagged?.LogError(message, duration);
	}

	public static void Custom(string message, Color backgroundColor, Color textColor, float duration = 2.5f)
	{
		_tagged?.LogCustomColors(message, textColor, backgroundColor, duration);
	}
}