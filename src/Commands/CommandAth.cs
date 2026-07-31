using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     The mod's front door: /ath on its own shows or hides the ATH window, from where
///     everything else can be reached without typing.
/// </summary>
public class CommandAth : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath";

	public string Description => "Shows or hides the Author-Time-Hunting window.";

	public void Handle(string arguments)
	{
		// Depending on how the SDK resolves overlapping commands, "/ath start" can arrive
		// here as "ath" with arguments. The specific commands own those; only the bare
		// form is ours.
		if (!string.IsNullOrWhiteSpace(arguments))
		{
			return;
		}

		Raise();
	}

	public static void Raise()
	{
		CommandTrigger?.Invoke();
	}

	public static event Action CommandTrigger;
}