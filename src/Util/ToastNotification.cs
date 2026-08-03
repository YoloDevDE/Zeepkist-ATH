using ZeepSDK.Messaging;

namespace AuthorTimeHunting.Util;

public static class Messenger
{
	// The tag is constant, so the messenger is too. Notify() used to build a new
	// TaggedMessenger on every call, and it is called from per-frame paths.
	private static readonly ITaggedMessenger Tagged = MessengerApi.CreateTaggedMessenger("ATH");

	public static ITaggedMessenger Notify()
	{
		return Tagged;
	}
}