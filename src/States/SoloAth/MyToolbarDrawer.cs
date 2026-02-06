using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;

namespace AuthorTimeHunting.States.SoloAth;

public class MyToolbarDrawer : IZeepToolbarDrawer
{
    public string MenuTitle => "ATH";

    public void DrawMenuItems(ImGui gui)
    {
        if (gui.Menu("Open Settings"))
        {
            // Open settings window
        }

        if (gui.Menu("Toggle Feature"))
        {
            // Toggle a feature
        }
    }
}