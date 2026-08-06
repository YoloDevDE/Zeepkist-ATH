using System;
using AuthorTimeHunting.Service;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     ATH's entry in the game's top bar. It is not a list of windows any more - it is the
///     same switch /ath is, and it opens the one screen everything else hangs off.
///     Eight checkboxes lived here before, one per window, which made the top bar a second
///     control panel that had to be kept in step with the first. The menu owns that job now.
///     ZeepSDK only hands a mod a dropdown, never a bare button, so this is a dropdown with a
///     single item in it - the closest thing to a button the toolbar allows.
/// </summary>
public class AthToolbar : IZeepToolbarDrawer
{
	private readonly ModServices _services;

	public AthToolbar(ModServices services)
	{
		_services = services;
	}

	public string MenuTitle => "ATH";

	public void DrawMenuItems(ImGui gui)
	{
		try
		{
			if (gui.Menu(Label().AsSpan()))
			{
				_services.Menu.Toggle();
			}
		}
		catch (Exception e)
		{
			Logger.LogError($"AthToolbar: Draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	private string Label()
	{
		return _services.Menu.Visible ? "Close Main Menu" : "Open Main Menu";
	}
}
