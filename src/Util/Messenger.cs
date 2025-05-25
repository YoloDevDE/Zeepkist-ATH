using ZeepSDK.Messaging;

namespace AuthorTimeHunting.Util;

public static class Messenger
{
    public static ITaggedMessenger Notify()
    {
        return MessengerApi.CreateTaggedMessenger("ATH");
    }
}