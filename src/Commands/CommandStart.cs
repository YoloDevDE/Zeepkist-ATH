using System;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Util;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandStart : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath start";

	public string Description => "Starts a hunt. Add a gamemode to pick one, e.g. /ath start classic.";

	/// <param name="arguments">
	///     The gamemode to play, or empty to use whatever is selected. Selecting here rather
	///     than passing the mode along the event chain keeps the trigger a plain Action - the
	///     UI's Start button and a typed command still take exactly the same path.
	/// </param>
	public void Handle(string arguments)
	{
		GamemodeRegistry registry = Plugin.Instance.Services.Gamemodes;

		if (!string.IsNullOrWhiteSpace(arguments))
		{
			IGamemode requested = registry.Resolve(arguments);

			if (requested == null)
			{
				ToastNotification.Warn($"No such gamemode. Known: {registry.IdList()}");
				return;
			}

			registry.Selected = requested;
		}

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