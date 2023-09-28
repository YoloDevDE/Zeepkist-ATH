using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class RestartChallengeCommand : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "hunt restart";

    public string Description =>
        "Restarts the AT Hunt. You can adjust the challenge-duration by adding the time in minutes for example: '/hunt restart 5' for 5 minutes";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke(arguments);
    }

    // Event-Definition
    public static event Action<string> OnHandle;
}