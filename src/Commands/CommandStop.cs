using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandStop : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath stop";
	public string Description => "Stop the Author-Time-Hunting.";

	public void Handle(string arguments)
	{
		CommandTrigger?.Invoke();
	}

	public static event Action CommandTrigger;
}