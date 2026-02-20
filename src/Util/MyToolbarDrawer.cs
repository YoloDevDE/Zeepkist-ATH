using Imui.Controls;
using Imui.Core;
using ZeepSDK.Chat;
using ZeepSDK.UI;

namespace AuthorTimeHunting.Util;

public class MyToolbarDrawer : IZeepToolbarDrawer
{
    public string MenuTitle => "ATH";

    public void DrawMenuItems(ImGui gui)
    {
        if (gui.Menu("Start")) { }

        if (gui.Menu("Stop"))
        {
            ChatApi.SendMessage("/ath stop");
        }
    }
}