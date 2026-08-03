using System;
using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using Imui.Controls;
using Imui.Core;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     ATH's menu in the game's top bar: one checkbox per window, and the two screens that are
///     opened rather than toggled.
///     Until now every window was reachable only through a chat command, which meant knowing
///     the command existed. The top bar is where a player looks for a mod's windows, and a
///     checkable menu item also answers the question a command cannot - whether the thing is
///     currently on.
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
			Toggle(gui, "Run HUD", () => _services.RunOverlay.Visible, value => _services.RunOverlay.Visible = value);
			Toggle(gui, "Current Level", () => _services.LevelStats.Visible,
				value => _services.LevelStats.Visible = value);
			Toggle(gui, "Controls", () => _services.Control.Visible, value => _services.Control.Visible = value);
			Toggle(gui, "Leaderboard", () => _services.Leaderboard.Visible,
				value => _services.Leaderboard.Visible = value);
			Toggle(gui, "Debug Panel", () => _services.Debug.Visible, value => _services.Debug.Visible = value);
			Toggle(gui, "Welcome Screen", () => _services.Welcome.Visible,
				value => _services.Welcome.Visible = value);
			Toggle(gui, "Help", () => _services.Help.Visible, value => _services.Help.Visible = value);

			if (gui.Menu("Match History".AsSpan()))
			{
				CommandAthHistory.Raise();
			}

			// Only offered when there is one up: a "close" that closes nothing is a dead entry
			// in a menu of four live ones.
			if (_services.Results.Visible && gui.Menu("Close Report".AsSpan()))
			{
				_services.Results.Close();
			}
		}
		catch (Exception e)
		{
			// Inside the game's shared GUI pass, same as every drawer.
			Logger.LogError($"AthToolbar: Draw failed: {e.Message}\n{e.StackTrace}");
		}
	}

	/// <summary>
	///     A checkable entry over a property. Imui takes the flag by reference and reports
	///     whether it was clicked, so the write only happens on a click - assigning every frame
	///     would fight anything else that sets the same property, and the run start does.
	/// </summary>
	private static void Toggle(ImGui gui, string label, Func<bool> get, Action<bool> set)
	{
		bool value = get();

		if (gui.Menu(label.AsSpan(), ref value))
		{
			set(value);
		}
	}
}
