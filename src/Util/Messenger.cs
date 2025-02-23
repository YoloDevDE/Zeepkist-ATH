using ZeepkistClient;
using ZeepSDK.Messaging;

namespace AuthorTimeHunting.Util;

public static class Messenger
{
    public static ITaggedMessenger Notify()
    {
        return MessengerApi.CreateTaggedMessenger("ATH");
    }

    public static void SendChat(string text)
    {
        ZeepkistNetwork.SendCustomChatMessage(false, ZeepkistNetwork.LocalPlayer.SteamID, "<br><color=#ffffff>" + text + "</color>", "---- Author Time Hunting ----");
    }
}