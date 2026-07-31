using AuthorTimeHunting.Util;
using ZeepkistClient;

namespace AuthorTimeHunting.Service;

public class ChatMessageService
{
    public static void SendCustomMessage(string message)
    {
        // Null after a disconnect - the stop path still tries to print the run summary.
        if (ZeepkistNetwork.LocalPlayer == null)
        {
            Logger.LogWarning("ChatMessageService: No local player, dropping chat message.");
            return;
        }

        ZeepkistNetwork.SendCustomChatMessage(false, ZeepkistNetwork.LocalPlayer.SteamID, message, "<#d0d0d0>---Author Time Hunting---</color>");
    }
}