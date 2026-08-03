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
		Raise();
	}

	/// <summary>
	///     Fires the command without going through chat, so the in-game UI and a typed
	///     command take exactly the same path.
	/// </summary>
	public static void Raise()
	{
		CommandTrigger?.Invoke();
	}

	public static event Action CommandTrigger;
}
