using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     Opens the developer panel. Its own command rather than a switch in the config, because
///     it is wanted for one run and not the next, and a config toggle costs a menu round trip
///     in the middle of the thing being debugged.
/// </summary>
public class CommandAthDebug : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "athdebug";

	public string Description => "Shows or hides the ATH debug panel.";

	public void Handle(string arguments)
	{
		Raise();
	}

	public static void Raise()
	{
		CommandTrigger?.Invoke();
	}

	public static event Action CommandTrigger;
}
