using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class SkipLevelCommand : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "hunt skip";

    public string Description =>
        "Skips the current level during the challenge. Uses a FreeSkip or GoldSkip. If no skips are available, a penalty is applied.";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }

    // Event-Definition
    public static event Action OnHandle;
}