using System;
using AuthorTimeHunting.Service;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The top bar's way into the developer panel. Its own entry rather than a line in the ATH
///     menu, so a player opening the mod's front door never trips over a button that hands out
///     author times.
/// </summary>
public class AthDebugToolbar : IZeepToolbarDrawer
{
	private readonly ModServices _services;

	public AthDebugToolbar(ModServices services)
	{
		_services = services;
	}

	public string MenuTitle => "ATH Debug";

	public void DrawMenuItems(ImGui gui)
	{
		try
		{
			if (gui.Menu(Label().AsSpan()))
			{
				_services.Debug.Toggle();
			}
		}
		catch (Exception e)
		{
			Logger.LogError($"AthDebugToolbar: Draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	private string Label()
	{
		return _services.Debug.Visible ? "Close Debug Panel" : "Open Debug Panel";
	}
}
