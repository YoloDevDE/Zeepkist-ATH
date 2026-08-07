using UnityEngine;
using ZeepSDK.Messaging;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Sends toast notifications to the top right corner of the game.
///     Call <see cref="Initialize" /> from the plugin before use.
/// </summary>
public static class FrogNotification
{
	private static ITaggedMessenger _tagged;

	public static string Tag => _tagged?.Tag;

	public static void Initialize(string tag)
	{
		_tagged = MessengerApi.CreateTaggedMessenger(tag);
	}

	public static void Info(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Style.Text.Default, duration);
	}

	public static void Success(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Style.Status.Positive, duration);
	}

	public static void Warn(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Style.Status.Negative, duration);
	}

	public static void Error(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Style.Status.Bad, duration);
	}

	public static void Author(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Zeepkist.Medal.Author, duration);
	}

	public static void Gold(string message, float duration = 2.5f)
	{
		Tinted(message, Color.Zeepkist.Medal.Gold, duration);
	}

	private static void Tinted(string message, Color32 textColor, float duration)
	{
		_tagged?.LogCustomColors(message, textColor, Color.Style.Surface.Panel, duration);
	}
}
