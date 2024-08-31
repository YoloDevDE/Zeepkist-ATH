using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class CommandRestart : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "ath restart";

    public string Description =>
        "Restarts the Author-Time-Hunting.";

    public void Handle(string arguments)
    {
        CommandTrigger?.Invoke();
    }

    // Event-Definition
    public static event Action CommandTrigger;
}