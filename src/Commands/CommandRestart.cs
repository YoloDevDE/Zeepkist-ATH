using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandRestart : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "ath restart";

    public string Description =>
        "Restarts the AT Hunt. You can adjust the challenge-duration by adding the time in minutes for example: '/hunt restart 5' for 5 minutes";

    public void Handle(string arguments)
    {
        CommandTrigger?.Invoke();
    }

    // Event-Definition
    public static event Action CommandTrigger;
}