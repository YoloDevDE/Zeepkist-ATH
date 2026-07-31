using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandStart : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath start";

	public string Description => "Starts the Author-Time-Hunting.";

	public void Handle(string arguments)
	{
		CommandTrigger?.Invoke();
	}

	// Event-Definition
	public static event Action CommandTrigger;
}