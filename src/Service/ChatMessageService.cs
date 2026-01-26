using ZeepkistClient;

namespace AuthorTimeHunting.Service;

public static class ChatMessageService
{
    public static void SendCustomMessage(string message)
    {
        ZeepkistNetwork.SendCustomChatMessage(false, ZeepkistNetwork.LocalPlayer.SteamID, message, "<#d0d0d0>---Author Time Hunting---</color>");
    }
}