using System;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using Imui.Style;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The hunt as a broadcast bar: a band across the top of the screen with the mod's crest in
///     the middle of it, the hour on the left of the crest and the level on the right, and the
///     controls in a drawer underneath.
///     It was a window in the middle of the top of the screen and it kept growing, until it was
///     eleven rows tall and sat exactly where a player looks on a jump. Flattening it to one line
///     fixed the height and broke the shape instead: a strip the full width of the screen is a
///     letterbox bar, and on an ultrawide it put the clock and the medals a monitor's width
///     apart. So the band is a fraction of the screen and centred, which is what a bar over a
///     game looks like everywhere else it is done.
///     The crest is the middle of it because that is what the middle is for. Neither wing carries
///     a number worth putting under the eye - the countdown a player actually drives to is the
///     ticker at the bottom of the screen, and this is the hour, which is read between attempts.
///     Nothing here is drawn before there is a level under the wheels. The bar used to come up
///     with the run, which meant it hung over the whole of the lobby setup with nothing in it but
///     dashes - and over the loading screen that exists to hide exactly that.
///     There is no visible window here, but there is a window. Imui draws text through the window
///     it is inside, and an earlier attempt at drawing a line of it against the bare canvas drew
///     nothing at all - so the frame is styled away rather than done without, the same way
///     <see cref="AthMenu" /> turns a window into a fullscreen screen. The band is then drawn by
///     hand, because a window box is a rectangle and this one has its corners cut off.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Bar";

	private const ImWindowFlag WindowFlags =
		ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton | ImWindowFlag.NoMovingAndResizing;

	private const float WidthFraction = 0.58f;
	private const float MinWidth = 640f;
	private const float MaxWidth = 1180f;

	/// <summary>How many rows of content the band holds, the rule along its bottom edge aside.</summary>
	private const float ContentRows = 2.7f;

	private const float RuleFraction = 0.16f;

	/// <summary>How much of the band's height its cut corners take. The bar's whole silhouette.</summary>
	private const float ChamferFraction = 0.45f;

	private const float BadgeAspect = 16f / 9f;

	private const float PennantWidthFraction = 0.34f;
	private const float PennantRows = 0.55f;

	private const float ClockShare = 0.55f;
	private const float ClockSize = 1.3f;

	/// <summary>
	///     How wide each block of a wing actually is, in rows.
	///     A wing is as wide as the band leaves it, which is far wider than anything standing in
	///     one. Spread to fill, three medal counts land a hand's width apart and stop reading as
	///     three medal counts, and the live dot ends up nearer the crest than the clock it belongs
	///     to. So every block takes what it needs off its own edge and leaves the rest empty.
	/// </summary>
	private const float ClockColumns = 6.4f;

	private const float MedalColumns = 7.4f;

	private const float LevelTimeColumns = 11.4f;

	private const float NameShare = 0.37f;
	private const float AuthorShare = 0.4f;

	/// <summary>Open or shut in a fifth of a second: too fast to wait for, too slow to be a jump cut.</summary>
	private const float SlideSeconds = 0.2f;

	/// <summary>One blink a second, lit for most of it. A recording light, not a turn signal.</summary>
	private const float BlinkOnFraction = 0.6f;

	/// <summary>Pixels the pointer has to travel in a frame before it counts as having been moved.</summary>
	private const float MotionThreshold = 1.5f;

	/// <summary>How long a still pointer stays "just used" before the drawer takes it back.</summary>
	private const float RestSeconds = 2.5f;

	private readonly ControlPanel _controls;

	private readonly AthThumbnail _thumbnail;

	private bool _mouseOverWindow;

	/// <summary>How far out the drawer is, 0 shut and 1 open. Everything about it follows from this.</summary>
	private float _open;

	private Vector3 _pointer;

	private float _stirred;

	public RunOverlay(ControlPanel controls, AthThumbnail thumbnail)
	{
		_controls = controls;
		_thumbnail = thumbnail;
	}

	/// <summary>
	///     Starting a hunt arms the bar; the first level is what puts it on screen. Everything the
	///     bar has to say comes off the level being driven, so between the two there is nothing to
	///     draw, and a band of empty dashes over the setup screen is worse than no band.
	/// </summary>
	public AthStateMachine ActiveRun
	{
		get;
		set
		{
			field = value;

			if (value != null)
			{
				Visible = true;
			}
		}
	}

	public bool Visible { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"RunOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	/// <summary>
	///     The drawer is sized before it is drawn, from how far open it was last frame, because the
	///     window it lives in has to be given its rect up front.
	///     The pennant is in the window's rect whether the drawer is out or not. It hangs below the
	///     band by design, and a window clips what is drawn in it - a rect that stopped at the band
	///     would cut the point off every time the drawer was shut.
	/// </summary>
	private void Draw(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = RunHudView.ForFrame(run);

		if (view == null)
		{
			return;
		}

		float row = gui.GetRowHeight();
		float pad = UiMetrics.Margin(gui) * 0.5f;
		float rule = Mathf.Max(3f, row * RuleFraction);
		float band = row * ContentRows + pad * 2f + rule;

		float full = _controls.Height(gui);
		float drawer = full * Advance();
		float below = Mathf.Max(row * PennantRows, drawer);

		ImRect screen = gui.Canvas.SafeScreenRect;
		float width = UiMetrics.Width(gui, WidthFraction, MinWidth, MaxWidth);
		ImRect rect = new(screen.X + (screen.W - width) * 0.5f, screen.Top - band - below, width, band + below);

		ImStyleWindow previous = gui.Style.Window;

		Bare(gui);

		try
		{
			DrawWindow(gui, rect, band, drawer, run, view);
		}
		finally
		{
			gui.Style.Window = previous;
		}
	}

	/// <summary>
	///     Takes the window's frame away entirely: the band is a cut-cornered shape drawn by hand,
	///     and anything the window painted underneath it would show at the corners as the rectangle
	///     the band is not.
	/// </summary>
	private static void Bare(ImGui gui)
	{
		gui.Style.Window.Box.BackColor = Color.clear;
		gui.Style.Window.Box.BorderThickness = 0f;
		gui.Style.Window.Box.BorderRadius = 0f;
		gui.Style.Window.ContentPadding = 0f;
	}

	/// <summary>
	///     Back to front, which in an immediate mode GUI is call order: the drawer first so it
	///     comes out from behind the band, then the band, then the crest over the seam between
	///     them, then everything that is words.
	/// </summary>
	private void DrawWindow(ImGui gui, ImRect rect, float band, float drawer, AthStateMachine run, RunHudView view)
	{
		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			ImRect area = gui.AddLayoutRect(gui.GetLayoutWidth(), rect.H);
			ImRect strip = area.TakeTop(band);
			float cut = Chamfer(strip);

			DrawDrawer(gui, new ImRect(area.X, strip.Y - drawer, area.W, drawer), cut, run, view);
			Chamfered(gui, strip, cut, Color.Style.Surface.Panel);
			DrawContent(gui, strip, cut, view);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     The bar's silhouette: a rectangle with its two bottom corners cut away. Imui's window
	///     box cannot do it, so it is six points and a convex fill - counter-clockwise, the winding
	///     Imui's own arrows use.
	///     The band and the drawer are both drawn with it, off the same cut, because the drawer is
	///     the band coming open rather than a second thing under it.
	/// </summary>
	private static void Chamfered(ImGui gui, ImRect rect, float cut, Color32 colour)
	{
		Span<Vector2> points = stackalloc Vector2[6];

		points[0] = new Vector2(rect.X + cut, rect.Y);
		points[1] = new Vector2(rect.Right - cut, rect.Y);
		points[2] = new Vector2(rect.Right, rect.Y + cut);
		points[3] = new Vector2(rect.Right, rect.Top);
		points[4] = new Vector2(rect.X, rect.Top);
		points[5] = new Vector2(rect.X, rect.Y + cut);

		gui.Canvas.ConvexFill(points, colour);
	}

	private static float Chamfer(ImRect rect)
	{
		return Mathf.Min(rect.H * ChamferFraction, rect.W * 0.03f);
	}

	private void DrawContent(ImGui gui, ImRect strip, float cut, RunHudView view)
	{
		float pad = UiMetrics.Margin(gui) * 0.5f;
		float rule = Mathf.Max(3f, gui.GetRowHeight() * RuleFraction);

		ImRect inner = strip.WithPadding(pad + cut, pad + cut, pad, rule + pad);
		float badgeWidth = inner.H * BadgeAspect;
		ImRect badge = new(inner.Center.x - badgeWidth * 0.5f, inner.Y, badgeWidth, inner.H);
		float wing = (inner.W - badgeWidth) * 0.5f - pad;

		DrawPennant(gui, strip, badgeWidth * PennantWidthFraction, gui.GetRowHeight() * PennantRows);
		DrawBadge(gui, badge);
		DrawHunt(gui, new ImRect(inner.X, inner.Y, wing, inner.H), view);
		DrawLevel(gui, new ImRect(inner.Right - wing, inner.Y, wing, inner.H), view);

		UiWidgets.Timeline(gui, new ImRect(strip.X + cut, strip.Y + pad * 0.4f, strip.W - cut * 2f, rule),
			view.Timeline);
	}

	/// <summary>
	///     The tab hanging off the middle of the band. It is the only part of the bar that is not
	///     information, and it is worth its pixels twice over: it is what says there is a drawer,
	///     and it is what stops the crest looking like it was dropped on the band by accident.
	/// </summary>
	private static void DrawPennant(ImGui gui, ImRect strip, float width, float height)
	{
		float half = width * 0.5f;
		float x = strip.Center.x;

		Span<Vector2> points = stackalloc Vector2[5];

		points[0] = new Vector2(x - half, strip.Y);
		points[1] = new Vector2(x - half, strip.Y - height * 0.45f);
		points[2] = new Vector2(x, strip.Y - height);
		points[3] = new Vector2(x + half, strip.Y - height * 0.45f);
		points[4] = new Vector2(x + half, strip.Y);

		gui.Canvas.ConvexFill(points, Color.Zeepkist.Medal.Gold);
	}

	/// <summary>The mod's own crest, or its initials where the plugin was built without the picture.</summary>
	private void DrawBadge(ImGui gui, ImRect rect)
	{
		float radius = rect.H * 0.14f;
		Texture2D logo = _thumbnail.Texture;

		gui.Canvas.Rect(rect, Color.Style.Surface.Tile, radius);
		gui.Canvas.RectOutline(rect, Color.Zeepkist.Medal.Gold, Mathf.Max(1f, rect.H * 0.045f), radius);

		if (logo == null)
		{
			UiText.Centre(gui, "ATH", Color.Zeepkist.Medal.Gold, rect, gui.Style.Layout.TextSize);

			return;
		}

		gui.Image(logo, rect.WithPadding(Mathf.Max(2f, rect.H * 0.09f)), true);
	}

	/// <summary>The left wing: the hour, whether it is being spent, and what has been bought with it.</summary>
	private static void DrawHunt(ImGui gui, ImRect rect, RunHudView view)
	{
		float row = gui.GetRowHeight();
		ImRect clock = rect.TakeTop(rect.H * ClockShare, out ImRect medals);

		DrawClock(gui, clock.TakeLeft(Mathf.Min(clock.W, row * ClockColumns)), view);
		DrawMedals(gui, medals.TakeLeft(Mathf.Min(medals.W, row * MedalColumns)), view);
	}

	private static void DrawClock(ImGui gui, ImRect rect, RunHudView view)
	{
		float icon = rect.H * 0.8f;
		float gap = gui.Style.Layout.InnerSpacing;

		ImRect watch = rect.TakeLeft(icon, gap, out ImRect rest);
		ImRect light = rest.TakeRight(icon, gap, out ImRect clock);

		UiIcons.Draw(gui, watch, UiIcon.Stopwatch, view.TimeColour);
		UiText.Draw(gui, view.TimeLeft, view.TimeColour, clock, gui.Style.Layout.TextSize * ClockSize, 0f);
		DrawLive(gui, light, view.Running);
	}

	/// <summary>
	///     The dot a live broadcast puts in the corner: on for most of every second, off for the
	///     rest of it, hard edges both ways. It is the only thing on the bar that says the hour is
	///     being spent right now rather than merely standing at some number, and a fade would read
	///     as breathing rather than as recording.
	///     Unscaled time, so the game slowing itself down cannot stretch the second.
	/// </summary>
	private static void DrawLive(ImGui gui, ImRect rect, bool running)
	{
		if (!running)
		{
			UiIcons.Draw(gui, rect, UiIcon.Stop, Color.Style.Status.Warning);

			return;
		}

		if (Time.unscaledTime % 1f > BlinkOnFraction)
		{
			return;
		}

		gui.Canvas.Circle(rect.Center, rect.H * 0.25f, Color.Style.Status.Alert);
	}

	private static void DrawMedals(ImGui gui, ImRect rect, RunHudView view)
	{
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 0, 3), GameSprites.AuthorMedal, view.AuthorMedals,
			Color.Zeepkist.Medal.Author);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 1, 3), GameSprites.GoldMedal, view.GoldMedals,
			Color.Zeepkist.Medal.Gold);
		UiWidgets.MedalCount(gui, UiWidgets.Column(gui, rect, 2, 3), GameSprites.YouTriedMedal, view.Penalties,
			Color.Style.Status.Penalty);
	}

	/// <summary>
	///     The right wing: what is being driven, whose it is, and the two times to beat. These four
	///     lines have been a window of their own, then a block in the middle of the view, then a
	///     card in the corner. Beside the crest is where they stop moving - they are the other half
	///     of what the bar is about, and a hunt with no level in front of it is not on screen.
	/// </summary>
	private static void DrawLevel(ImGui gui, ImRect rect, RunHudView view)
	{
		float text = gui.Style.Layout.TextSize;

		ImRect name = rect.TakeTop(rect.H * NameShare, out ImRect rest);
		ImRect author = rest.TakeTop(rest.H * AuthorShare, out ImRect times);

		UiText.Right(gui, view.LevelName, Color.Style.Text.LevelName, name, text * 1.05f);
		UiText.Right(gui, view.ByAuthor, Color.Style.Text.AuthorName, author, text * 0.8f);

		DrawTimes(gui, times.TakeRight(Mathf.Min(times.W, gui.GetRowHeight() * LevelTimeColumns)), view);
	}

	private static void DrawTimes(ImGui gui, ImRect rect, RunHudView view)
	{
		DrawMedalTime(gui, UiWidgets.Column(gui, rect, 0, 2), GameSprites.AuthorMedal, view.AuthorTime,
			Color.Zeepkist.Medal.Author);
		DrawMedalTime(gui, UiWidgets.Column(gui, rect, 1, 2), GameSprites.GoldMedal, view.GoldTime,
			Color.Zeepkist.Medal.Gold);
	}

	private static void DrawMedalTime(ImGui gui, ImRect cell, Sprite sprite, string time, Color32 colour)
	{
		ImRect icon = cell.TakeLeft(cell.H, gui.Style.Layout.InnerSpacing, out ImRect text);

		UiWidgets.Medal(gui, icon, sprite, colour);
		UiText.Draw(gui, time, colour, text, gui.Style.Layout.TextSize * 0.9f, 0f);
	}

	/// <summary>
	///     The controls, always laid out at their full height against the underside of the band,
	///     and clipped to however far the drawer has come out. That is what makes it read as a
	///     drawer rather than as a fade: the buttons arrive from behind the band with their far
	///     edge still cut off by it.
	///     The clip does the input too. Imui throws away a hover that falls outside the active clip
	///     rect, so a button half out of the slot is live on exactly the half that can be seen.
	/// </summary>
	private void DrawDrawer(ImGui gui, ImRect rect, float cut, AthStateMachine run, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		float full = _controls.Height(gui);
		float pad = UiMetrics.Margin(gui);

		gui.Canvas.PushClipRect(rect);

		try
		{
			Chamfered(gui, rect, Mathf.Min(cut, rect.H), Color.Style.Surface.Panel);
			_controls.Draw(gui, new ImRect(rect.X + pad, rect.Top - full, rect.W - pad * 2f, full), run, view);
		}
		finally
		{
			gui.Canvas.PopClipRect();
		}
	}

	private float Advance()
	{
		float step = Time.unscaledDeltaTime / SlideSeconds;

		_open = Mathf.Clamp01(_open + (PointerActive() ? step : -step));

		return _open;
	}

	/// <summary>
	///     Whether the pointer has been used lately, which is the question the drawer actually
	///     wants answered. Opening on hover meant aiming at a strip of screen to reach the buttons
	///     behind it, and the strip is at the very top edge, which is the one place a pointer
	///     cannot overshoot into - so it was a drawer that only opened for somebody who already
	///     knew where it was.
	///     Moving the mouse at all is the honest signal here: this is a racing game, the pointer
	///     does not steer anything, and a hand that has gone to it has gone to it for the buttons.
	/// </summary>
	private bool PointerActive()
	{
		Vector3 now = Input.mousePosition;
		bool moved = (now - _pointer).sqrMagnitude > MotionThreshold * MotionThreshold;

		_pointer = now;

		if (moved)
		{
			_stirred = Time.unscaledTime;
		}

		return Time.unscaledTime - _stirred < RestSeconds;
	}
}
