using System;
using ZeepSDK.ChatCommands;

namespace AuthorTimeHunting.Commands;

public class SkipBrokenLevelCommand : ILocalChatCommand
{
    public string Prefix => "/";
    public string Command => "hunt broken";

    public string Description =>
        "Skips the current level in the challenge without incurring a penalty. Use this only if the level is unplayable. Please adhere to the rules!";

    public void Handle(string arguments)
    {
        OnHandle?.Invoke();
    }

    // Event-Definition
    public static event Action OnHandle;
}