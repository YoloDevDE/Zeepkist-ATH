using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     Writes the current level off as broken and skips it, the way the Broken button does.
///     Registered only while a level is actually being raced - not while one loads - so the
///     command cannot land on a level the run has no time for yet.
/// </summary>
public class CommandAthBroken : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath broken";

	public string Description => "Marks the current level as broken and skips it.";

	public void Handle(string arguments)
	{
		AthRequests.SkipBroken();
	}
}
