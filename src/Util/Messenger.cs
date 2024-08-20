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
        ChatApi.ClearChat();
        ChatApi.SendMessage(text);
    }
}