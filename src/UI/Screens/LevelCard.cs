using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     What is being driven and what it takes: the level, whose it is, and the two times to
///     beat.
///     These four lines have moved twice. They were a window called "Current Level" that a
///     player had to go and open, which meant nobody read them; then they were folded into the
///     bar across the top, which is where they turned that bar into a block in the middle of the
///     view. The corner is the third answer and the right one - the times are looked at between
///     attempts rather than mid-air, so they want a place that is out of the way rather than one
///     that is under the eye.
///     The bottom right corner is free because the controls left it for the bar's drawer.
///     There is no attempt counter here. The ticker at the bottom of the screen writes the
///     attempt above every countdown, and a second copy of a number that is already on screen is
///     how the old panel got to be eleven rows tall.
/// </summary>
public class LevelCard : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Level";

	private const float WidthFraction = 0.17f;

	private const float MinWidth = 260f;
	private const float MaxWidth = 380f;

	private const float LevelNameSize = 1.1f;
	private const float AuthorSize = 0.85f;
	private const float MedalRowSize = 1.2f;

	private const ImWindowFlag WindowFlags =
		ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton | ImWindowFlag.NoResizing;

	private float _contentHeight;

	private bool _mouseOverWindow;

	public AthStateMachine ActiveRun { get; set; }

	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		RunHudView view = RunHudView.ForFrame(ActiveRun);

		if (view == null)
		{
			return;
		}

		try
		{
			Draw(gui, view);
		}
		catch (Exception e)
		{
			Logger.LogError($"LevelCard: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	private void Draw(ImGui gui, RunHudView view)
	{
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);

		ImRect rect = ImWindowPlacement.PlaceAutoSized(gui, WindowTitle.AsSpan(), width,
			UiMetrics.WindowHeight(gui, _contentHeight, 5f), ImWindowAnchor.BottomRight);

		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			DrawBody(gui, view);

			_contentHeight = UiMetrics.ContentHeight(gui);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	private static void DrawBody(ImGui gui, RunHudView view)
	{
		float text = gui.Style.Layout.TextSize;

		UiText.Draw(gui, view.LevelName, Color.Style.Text.LevelName, UiMetrics.Row(gui, LevelNameSize * 1.2f),
			text * LevelNameSize, 0f);
		UiText.Draw(gui, view.ByAuthor, Color.Style.Text.AuthorName, UiMetrics.Row(gui, 0.9f), text * AuthorSize, 0f);

		DrawMedalTime(gui, GameSprites.AuthorMedal, "AT", view.AuthorTime, Color.Zeepkist.Medal.Author);
		DrawMedalTime(gui, GameSprites.GoldMedal, "Gold", view.GoldTime, Color.Zeepkist.Medal.Gold);
	}

	private static void DrawMedalTime(ImGui gui, Sprite sprite, string label, string time, Color32 colour)
	{
		ImRect row = UiMetrics.Row(gui, MedalRowSize);
		float iconSize = row.H;

		ImRect icon = row.TakeLeft(iconSize, gui.Style.Layout.InnerSpacing, out ImRect rest);

		DrawMedalIcon(gui, icon, sprite, iconSize, colour);
		UiWidgets.Row(gui, rest, label, time, colour);
	}

	/// <summary>
	///     The game's own medal, or a dot in its colour where the game has not loaded its art yet.
	///     Every sprite off PlayerManager can be null and this is drawn from the first frame of a
	///     level, so the fallback is the ordinary case rather than the broken one.
	/// </summary>
	private static void DrawMedalIcon(ImGui gui, ImRect icon, Sprite sprite, float iconSize, Color32 colour)
	{
		if (sprite == null)
		{
			gui.Canvas.Circle(icon.Center, iconSize * 0.3f, colour);

			return;
		}

		gui.Image(sprite, icon, true);
	}
}
