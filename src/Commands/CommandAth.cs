using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     The mod's only chat command, and its front door: /ath shows or hides the ATH window,
///     from where everything else can be reached without typing.
///     It does exactly what the top bar's ATH entry does, on purpose - there is one way in,
///     and a player who found either one has found the whole mod.
/// </summary>
public class CommandAth : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath";

	public string Description => "Shows or hides the Author-Time-Hunting window.";

	public void Handle(string arguments)
	{
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
