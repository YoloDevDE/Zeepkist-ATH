using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The five things a player does to a running hunt: skip, report the level broken, pause,
///     restart, stop.
///     It is not a window any more. It used to own a corner of the screen and hold it all hour
///     for five buttons that get pressed a handful of times, so it moved into the drawer under
///     <see cref="RunOverlay" /> and is drawn into whatever rect the bar hands it. That is why
///     there is no Visible here and no placement: the bar decides when and where, this decides
///     what.
///     Each button is its symbol over its word. They were labelled buttons first, which is what
///     made the panel as wide as it was; then they were symbols alone, which made them five
///     identical boxes in a row - a shape is only worth a word once it has been learned, and five
///     presses an hour is not enough to learn one. The tile is both, and it is the smallest thing
///     that is.
///     The price of a skip is written above the row. It is the one thing here that changes hour
///     to hour - free, gold, penalty, fatal - and the one thing no icon can carry.
/// </summary>
public class ControlPanel
{
	private const int _buttons = 5;

	private const string _stopLabel = "Stop";

	/// <summary>
	///     Barely wider than tall. The word underneath the symbol is what sets the width, and the
	///     tile was wider than that when the symbol was a triangle drawn to fill its box: an icon
	///     from a set is squared inside the room left over the caption, so a wide tile spends its
	///     width on air beside a small icon rather than on the icon.
	/// </summary>
	private const float _tileAspect = 1.05f;

	/// <summary>
	///     Half of what it was. The drawer holds five buttons pressed a handful of times an hour,
	///     and at its old size it came out over a third of the screen to say so - which is a lot of
	///     game covered up by a thing whose whole point is that it is not there most of the time.
	/// </summary>
	private const float _tileRows = 1.25f;

	/// <summary>The skip price above the row, shrunk with the tiles so the drawer stays one block.</summary>
	private const float _captionRows = 0.75f;

	private float _height;

	/// <summary>
	///     How far the drawer has to open to show all of this. Taken off the last frame that drew
	///     it, because the bar has to size the drawer before anything is in it - the guess in rows
	///     only ever stands until the first measurement.
	/// </summary>
	public float Height(ImGui gui)
	{
		float fallback = gui.GetRowHeight() * (_tileRows + _captionRows + 0.4f);

		return (_height > 0f ? _height : fallback) + UiMetrics.Margin(gui);
	}

	public void Draw(ImGui gui, ImRect rect, AthController run, RunHudView view)
	{
		gui.Layout.Push(ImAxis.Vertical, rect);

		try
		{
			DrawControls(gui, run, view);

			_height = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.Layout.Pop();
		}
	}

	private static void DrawControls(ImGui gui, AthController run, RunHudView view)
	{
		UiText.Centre(gui, view.SkipType, view.SkipColour, UiMetrics.Row(gui, _captionRows),
			gui.Style.Layout.TextSize * 0.8f);

		Buttons(gui, Strip(gui), run, view, _stopLabel);
	}

	/// <summary>
	///     The five buttons themselves, laid into whatever strip they are handed. The main menu puts
	///     the same five up while a hunt is running, and a second copy of them would be five chances
	///     for the two to drift apart - one of them keeping a colour the other lost, or still
	///     sending /fs after the drawer stopped.
	///     Only the last one is named differently. On the drawer, above a bar that is plainly the
	///     run, "Stop" is enough; on a menu that also has a Quit on it, the button that ends the hour
	///     has to say which of the two it is.
	/// </summary>
	public static void Buttons(ImGui gui, ImRect strip, AthController run, RunHudView view, string stopLabel)
	{
		bool racing = GameStateObserver.IsRacing;

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 0), UiIcon.Skip, "Skip", Color.Style.Action.Skip, racing))
		{
			ChatApi.SendMessage("/fs");
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 1), UiIcon.Warning, "Broken", Color.Style.Action.Broken, racing))
		{
			AthRequests.SkipBroken();
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 2), view.Paused ? UiIcon.Play : UiIcon.Pause,
			    view.Paused ? "Resume" : "Pause",
			    view.Paused ? Color.Style.Action.Resume : Color.Style.Action.Pause, true))
		{
			TogglePause(run, view);
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 3), UiIcon.Restart, "Restart", Color.Style.Action.Restart, true))
		{
			AthRequests.Restart();
		}

		if (UiWidgets.IconTile(gui, Tile(gui, strip, 4), UiIcon.Stop, stopLabel, Color.Style.Action.Stop, true))
		{
			AthRequests.Stop();
		}
	}

	private static void TogglePause(AthController run, RunHudView view)
	{
		if (view.Paused)
		{
			run.ResumeRun();

			return;
		}

		run.PauseRun();
	}

	/// <summary>
	///     Five tiles in the middle of the drawer rather than five stretched across it. The bar is
	///     wide, and a stop button a bar wide is not a button anybody aims at - it is a region you
	///     fall into.
	/// </summary>
	private static ImRect Strip(ImGui gui)
	{
		float height = gui.GetRowHeight() * _tileRows;

		return Centred(gui, gui.AddLayoutRectWithSpacing(gui.GetLayoutWidth(), height), height);
	}

	/// <summary>
	///     Five tiles of the given height, centred in the row. The menu draws them taller than the
	///     drawer does - it has the whole screen and the drawer has a strip of it - so the height is
	///     the caller's to pick and everything else follows from it.
	/// </summary>
	public static ImRect Centred(ImGui gui, ImRect row, float height)
	{
		float side = height * _tileAspect;
		float gap = gui.Style.Layout.InnerSpacing;
		float width = side * _buttons + gap * (_buttons - 1);

		return new ImRect(row.X + (row.W - width) * 0.5f, row.Y, width, height);
	}

	private static ImRect Tile(ImGui gui, ImRect strip, int index)
	{
		float side = strip.H * _tileAspect;
		float gap = gui.Style.Layout.InnerSpacing;

		return new ImRect(strip.X + index * (side + gap), strip.Y, side, strip.H);
	}
}
