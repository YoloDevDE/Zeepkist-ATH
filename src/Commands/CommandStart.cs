using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandStart : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "ath start";

    public string Description =>
        "Starts the AT Hunt. You can adjust the challenge-duration by adding the time in minutes for example: /hunt start 60";

    public void Handle(string arguments)
    {
        CommandTrigger?.Invoke();
    }

    // Event-Definition
    public static event Action CommandTrigger;
}