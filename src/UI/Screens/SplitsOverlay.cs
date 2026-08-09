using System;
using System.Collections.Generic;
using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Rendering;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The run so far, checkpoint by checkpoint, down the left edge of the screen.
///     <code>
///     SPLITS
///     ─────────────────────────
///     1    00:04.221   -0.113  78
///     2    00:09.874   +0.042  64
///     3    --:--.---
///     ─────────────────────────
///     F    --:--.---
///     </code>
///     The game says the same thing for one second and then takes it away. A popup answers "was
///     that checkpoint good", which is the small question; the list answers "is this run still
///     good", which is the one that decides whether to keep driving or reset - and that answer
///     is three checkpoints wide, so it has to stay on screen.
///     Left, because the top is taken and the right is the leaderboard. It is also the side a
///     right-handed track rarely puts anything on, and the eye is already there for the mirror
///     of the ticker.
///     It is built the way the bar is: no window frame, a hand-drawn shape with its inward
///     corners cut, and an arrival from off the edge it is docked to. Those are not decorations
///     copied across - they are what makes two panels on two edges of the screen read as one mod
///     rather than as two overlays that happened to install together.
///     What it is measured against is the best attempt of this hunt on this level, recorded by
///     <see cref="Level.RecordRun" />. Not the game's own reference: that one is switched by a
///     game setting and stored on the network player, so it can quietly be somebody else's idea
///     of a personal best.
/// </summary>
public class SplitsOverlay : IZeepGUIDrawer
{

	private const string _heading = "SPLITS";


	private const float _widthFraction = 0.18f;
	private const float _minWidth = 240f;
	private const float _maxWidth = 380f;

	/// <summary>The heading, the two rules and the finish row, in rows, on top of the checkpoints.</summary>
	private const float _chromeRows = 2.7f;

	private const float _ruleRows = 0.35f;

	/// <summary>
	///     How much of the screen the list may take before its rows start shrinking. A level with
	///     thirty checkpoints is rare and would otherwise draw a column taller than the game.
	/// </summary>
	private const float _heightCap = 0.7f;

	/// <summary>How much of the panel's short side its cut corners take, the way the bar's do.</summary>
	private const float _chamferFraction = 0.09f;

	/// <summary>The same fifth of a second the bar and its drawer move in.</summary>
	private const float _slideSeconds = 0.2f;

	private static readonly float[] _weights = [0.13f, 0.42f, 0.28f, 0.17f];

	/// <summary>The corner points of the panel's shape, written afresh every frame rather than allocated.</summary>
	private static readonly Vector2[] _points = new Vector2[6];

	/// <summary>How far in the panel has come, 0 off the edge of the screen and 1 at rest.</summary>
	private float _in;


	public AthController ActiveRun { get; set; }

	public bool Visible { get; set; } = true;

	public void OnZeepGUI(ImGui gui)
	{
		AthController run = ActiveRun;

		if (!Visible || run?.Ctx.CurrentLevel == null || !Plugin.Instance.MyConfig.Splits.Value)
		{
			_in = 0f;

			return;
		}

		try
		{
			Paint(gui, run.Ctx);
		}
		catch (Exception e)
		{
			Logger.LogError($"SplitsOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	/// <summary>
	///     Painted straight onto the canvas, with no window anywhere near it. The first version was
	///     an Imui window with everything that makes one a window styled away, and it never showed
	///     up in the game at all: a window is registered with the window manager, and one whose
	///     rect starts off the edge of the screen - which is where this one arrives from - is not a
	///     window that manager is willing to lay out.
	///     Nothing here needs a window anyway. There is nothing to click, nothing to drag and
	///     nothing to scroll; it is a shape and some text, which is exactly what the canvas draws.
	///     Left at the default order, so the mod's own screens still cover it - a menu with the
	///     splits showing through it would be the same bug the other way round.
	/// </summary>
	private void Paint(ImGui gui, AthCtx ctx)
	{
		Level level = ctx.CurrentLevel;

		IReadOnlyList<SplitRow> rows = SplitsTable.Build(GameStateObserver.CheckpointCount,
			GameStateObserver.CurrentSplits, level.BestSplits, FinishTime(ctx), level.PersonalBestTime);

		float line = Line(gui, rows.Count);
		float pad = UiMetrics.Margin(gui) * 0.5f;
		float height = line * (rows.Count + _chromeRows) + pad * 2f;

		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = UiMetrics.Width(gui, _widthFraction, _minWidth, _maxWidth);

		ImRect rect = new(Mathf.Lerp(screen.Left - width, screen.Left, Slide()),
			screen.Bottom + (screen.H - height) * 0.5f, width, height);

		float cut = Mathf.Min(rect.W, rect.H) * _chamferFraction;

		Chamfered(gui, rect, cut);
		DrawList(gui, rect.WithPadding(pad, pad + cut, pad, pad), line, rows);
	}

	/// <summary>
	///     A rectangle with the two corners facing into the screen cut away - the bar's silhouette
	///     turned on its side. The edge against the screen is left square, because that is the edge
	///     the panel came in through and a shape cut on all four sides floats rather than docks.
	/// </summary>
	private static void Chamfered(ImGui gui, ImRect rect, float cut)
	{
		_points[0] = new Vector2(rect.X, rect.Y);
		_points[1] = new Vector2(rect.Right - cut, rect.Y);
		_points[2] = new Vector2(rect.Right, rect.Y + cut);
		_points[3] = new Vector2(rect.Right, rect.Top - cut);
		_points[4] = new Vector2(rect.Right - cut, rect.Top);
		_points[5] = new Vector2(rect.X, rect.Top);

		gui.Canvas.ConvexFill(_points, Color.Style.Surface.Panel);
	}

	/// <summary>
	///     How far in the panel has come. It arrives from off the left edge rather than appearing
	///     there, because a block of numbers that is simply on screen one frame later reads as a
	///     glitch - and because the bar drops in from the top, so the two of them are the same mod
	///     arriving rather than two things happening.
	/// </summary>
	private float Slide()
	{
		_in = Mathf.Clamp01(_in + Time.unscaledDeltaTime / _slideSeconds);

		return _in;
	}

	/// <summary>
	///     Rows first, finish last, with a rule between them - the finish is not a checkpoint and
	///     is not measured against one, so it does not stand in the same column of numbers.
	/// </summary>
	private static void DrawList(ImGui gui, ImRect area, float line, IReadOnlyList<SplitRow> rows)
	{
		UiColumn column = new(area, line);

		Text(gui, _heading, Color.Style.Text.Muted, column.Row(1f), gui.Style.Layout.TextSize * 0.8f, 0f);
		Rule(gui, column.Row(_ruleRows));

		for (int i = 0; i < rows.Count - 1; i++)
		{
			DrawRow(gui, column.Row(1f), rows[i]);
		}

		Rule(gui, column.Row(_ruleRows));
		DrawRow(gui, column.Row(1f), rows[rows.Count - 1]);
	}

	private static void DrawRow(ImGui gui, ImRect row, SplitRow split)
	{
		float size = gui.Style.Layout.TextSize;
		bool driven = split.Time != SplitsTable.NoTime;

		Text(gui, split.Label, Color.Style.Text.Muted, Cell(row, 0), size * 0.9f, 0f);
		Text(gui, split.Time, driven ? Color.Style.Surface.White : Color.Style.Text.Muted, Cell(row, 1), size,
			0f);
		Text(gui, split.Gap, PaceColour(split.Pace), Cell(row, 2), size, 1f);
		Text(gui, split.Speed, PaceColour(split.SpeedPace), Cell(row, 3), size * 0.9f, 1f);
	}

	/// <summary>
	///     A line of text on the bare canvas. <see cref="UiText" /> writes through the window it is
	///     inside and there is no window here, so this is the same job done one layer down.
	/// </summary>
	private static void Text(ImGui gui, string text, Color32 colour, ImRect rect, float size, float alignX)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		ImTextSettings settings = new(size, alignX, 0.5f, false, ImTextOverflow.Ellipsis);

		gui.Canvas.Text(text.AsSpan(), colour, rect, in settings);
	}

	private static ImRect Cell(ImRect row, int column)
	{
		return UiWidgets.Cell(row, _weights, column);
	}

	/// <summary>
	///     Green for ahead and red for behind, and the muted grey where there is nothing to
	///     compare against - which is the first run on every level of a hunt, so it has to read
	///     as "no answer yet" rather than as a verdict.
	/// </summary>
	private static Color32 PaceColour(SplitPace pace)
	{
		switch (pace)
		{
			case SplitPace.Ahead:
				return Color.Style.Status.Positive;
			case SplitPace.Behind:
				return Color.Style.Status.Bad;
			default:
				return Color.Style.Text.Muted;
		}
	}

	private static void Rule(ImGui gui, ImRect rect)
	{
		float thickness = Mathf.Max(1f, rect.H * 0.2f);

		gui.Canvas.Rect(new ImRect(rect.X, rect.Y + (rect.H - thickness) * 0.5f, rect.W, thickness),
			Color.Style.Surface.Track);
	}

	/// <summary>
	///     How tall one row is allowed to be. The theme's own height until the list would run off
	///     the screen, and whatever fits after that - a short row is readable, a row below the
	///     bottom edge is not.
	/// </summary>
	private static float Line(ImGui gui, int rows)
	{
		float natural = gui.GetRowHeight();
		float lines = rows + _chromeRows;
		float allowed = gui.Canvas.SafeScreenRect.H * _heightCap - UiMetrics.WindowChrome(gui);

		return natural * lines <= allowed ? natural : allowed / lines;
	}

	/// <summary>
	///     The time on the board, and only while it is on the board. The level's clock stops
	///     between a scored finish and the next attempt, which is exactly the window in which the
	///     finish row has something true to say - the same test <see cref="Hud.RaceTimeDisplay" />
	///     makes.
	/// </summary>
	private static double FinishTime(AthCtx ctx)
	{
		return ctx.CurrentLevel.IsTiming ? -1d : ctx.LastRunTime;
	}
}
