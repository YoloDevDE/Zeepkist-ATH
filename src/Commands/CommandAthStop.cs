using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     Ends the running hunt. Registered only while one exists - see
///     <see cref="States.Master.States.StateMasterOn" /> - so the command is absent rather than
///     inert when there is nothing to stop.
/// </summary>
public class CommandAthStop : ILocalChatCommand
{
	public string Prefix => "/";
	public string Command => "ath stop";

	public string Description => "Ends the running hunt.";

	public void Handle(string arguments)
	{
		AthRequests.Stop();
	}
}
