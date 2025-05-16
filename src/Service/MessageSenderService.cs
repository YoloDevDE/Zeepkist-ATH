using ZeepkistClient;

namespace AuthorTimeHunting.Service;

public class MessageSenderService
{
    public static void SendLocalMessage(string message)
    {
        ZeepkistNetwork.SendCustomChatMessage(false, ZeepkistNetwork.LocalPlayer.SteamID, message, "<#d0d0d0>---Author Time Hunting---</color>");
    }
}