using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class StartChallengeCommand : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "ath start";

    public string Description =>
        "Starts the AT Hunt. You can adjust the challenge-duration by adding the time in seconds for example: /ath start 600";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke(arguments);
    }

    // Event-Definition
    public static event Action<string> OnHandle;
}