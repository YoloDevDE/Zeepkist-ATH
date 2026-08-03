using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandSkipBroken : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath broken";

	public string Description =>
		"Skips the current level in the challenge without incurring a penalty. Use this only if the level is unplayable. Please adhere to the rules!";

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

	// Event-Definition
	public static event Action CommandTrigger;
}
