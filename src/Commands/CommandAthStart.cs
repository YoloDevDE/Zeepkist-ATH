using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     Starts a hunt without opening a window: the same quickstart the menu's first tile does,
///     on the settings ATH has always had.
///     Always registered. Starting while a hunt is already live is answered by the run itself.
/// </summary>
public class CommandAthStart : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath start";

	public string Description => "Starts a classic Author-Time-Hunting run.";

	public void Handle(string arguments)
	{
		Plugin.Instance.Services.Menu.Quickstart();
	}
}
