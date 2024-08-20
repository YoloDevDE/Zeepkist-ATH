using ZeepSDK.Chat;
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
        ChatApi.SendMessage(text);
    }
}