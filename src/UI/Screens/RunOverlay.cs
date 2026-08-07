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
///     The hunt, as one line along the top edge of the screen, with the controls in a drawer
///     underneath it.
///     It was a window in the middle of the top of the screen and it kept growing. Everything
///     worth knowing that had nowhere else to live moved in, until it was eleven rows tall and
///     sat exactly where a player looks on a jump. So the run's own numbers - how much hour is
///     left, whether it is being spent, how it has been spent, what has been won with it - are a
///     strip one line high that no amount of new information can inflate, and the rest was given
///     places of its own: the level to <see cref="LevelCard" /> in the corner, the attempt and
///     the medal gaps to the ticker at the bottom, which was already saying them.
///     The buttons hang under it in a drawer that opens when the pointer arrives and slides shut
///     when it leaves. Skip, restart and stop are pressed a handful of times an hour and were
///     costing a permanent panel; now they cost nothing until a hand is on its way to them.
///     There is no visible window here, but there is a window. Imui draws text through the
///     window it is inside, and an earlier attempt at drawing a line of it against the bare
///     canvas drew nothing at all - so the frame is styled away rather than done without, the
///     same way <see cref="AthMenu" /> turns a window into a fullscreen screen.
/// </summary>
public class RunOverlay : IZeepGUIDrawer
{
	private const string WindowTitle = "ATH Bar";

	private const ImWindowFlag WindowFlags =
		ImWindowFlag.NoTitleBar | ImWindowFlag.NoCloseButton | ImWindowFlag.NoMovingAndResizing;

	private const float ClockSize = 1.4f;

	/// <summary>How much of the bar the hunt's own block gets before the timeline takes the rest.</summary>
	private const float StatusFraction = 0.34f;

	/// <summary>Open or shut in a fifth of a second: too fast to wait for, too slow to be a jump cut.</summary>
	private const float SlideSeconds = 0.2f;

	/// <summary>One blink a second, lit for most of it. A recording light, not a turn signal.</summary>
	private const float BlinkOnFraction = 0.6f;

	private readonly ControlPanel _controls;

	private bool _mouseOverWindow;

	/// <summary>How far out the drawer is, 0 shut and 1 open. Everything about it follows from this.</summary>
	private float _open;

	public RunOverlay(ControlPanel controls)
	{
		_controls = controls;
	}

	/// <summary>
	///     Starting a hunt puts the bar up and leaves it up, which is what the controls did when
	///     they were their own panel: the drawer is where the next hunt is started from, so it has
	///     to still be there once this one is over.
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
	///     window it lives in has to be given its rect up front. <see cref="_mouseOverWindow" /> is
	///     last frame's answer for the same reason - which is the right answer anyway, since it was
	///     taken against the window at the size the player was actually pointing at.
	/// </summary>
	private void Draw(ImGui gui)
	{
		AthStateMachine run = ActiveRun;
		RunHudView view = RunHudView.ForFrame(run);
		bool idle = view == null;

		ImRect screen = gui.Canvas.SafeScreenRect;

		float bar = gui.GetRowHeight() * ClockSize + UiMetrics.Margin(gui);
		float full = _controls.Height(gui, idle);
		float drawer = full * Advance(idle);

		ImRect rect = new(screen.X, screen.Top - bar - drawer, screen.W, bar + drawer);
		ImStyleWindow previous = gui.Style.Window;

		Strip(gui);

		try
		{
			DrawWindow(gui, rect, bar, full, run, view);
		}
		finally
		{
			gui.Style.Window = previous;
		}
	}

	/// <summary>
	///     Turns the window into a strip: no border, no corners, no padding of its own. The bar has
	///     to sit against the very edge of the screen, and a window's chrome is what would stop it.
	/// </summary>
	private static void Strip(ImGui gui)
	{
		gui.Style.Window.Box.BackColor = Color.Style.Surface.Panel;
		gui.Style.Window.Box.BorderThickness = 0f;
		gui.Style.Window.Box.BorderRadius = 0f;
		gui.Style.Window.ContentPadding = 0f;
	}

	private void DrawWindow(ImGui gui, ImRect rect, float bar, float full, AthStateMachine run, RunHudView view)
	{
		bool open = true;

		if (!gui.BeginWindow(WindowTitle, ref open, ref _mouseOverWindow, rect, WindowFlags))
		{
			return;
		}

		try
		{
			ImRect strip = gui.AddLayoutRect(gui.GetLayoutWidth(), bar);

			DrawBar(gui, Padded(gui, strip), view);
			DrawGrip(gui, strip);
			DrawDrawer(gui, gui.AddLayoutRect(gui.GetLayoutWidth(), rect.H - bar), full, run, view);
		}
		finally
		{
			gui.EndWindow();
		}
	}

	/// <summary>
	///     The left third is the hunt itself - the hour, whether it is running, and what has been
	///     won with it. The rest is the timeline, because it is the one thing here that is worth
	///     more the wider it is drawn.
	/// </summary>
	private static void DrawBar(ImGui gui, ImRect rect, RunHudView view)
	{
		if (view == null)
		{
			UiText.Left(gui, "No hunt running.", Color.Style.Text.Muted, rect);

			return;
		}

		ImRect status = rect.TakeLeft(rect.W * StatusFraction, UiMetrics.Margin(gui), out ImRect timeline);
		ImRect clock = status.TakeLeft(gui.GetRowHeight() * 5.6f, gui.Style.Layout.InnerSpacing, out ImRect medals);

		DrawClock(gui, clock, view);
		DrawMedals(gui, medals, view);

		UiWidgets.Timeline(gui, Hairline(timeline), view.Timeline);
	}

	private static void DrawClock(ImGui gui, ImRect rect, RunHudView view)
	{
		float icon = rect.H * 0.7f;

		ImRect watch = rect.TakeLeft(icon, gui.Style.Layout.InnerSpacing, out ImRect rest);
		ImRect light = rest.TakeRight(icon, gui.Style.Layout.InnerSpacing, out ImRect clock);

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
	///     The handle every drawer has, so there is something to say one is there. It is the only
	///     part of the bar that is not information, and it is worth its two pixels: a drawer nobody
	///     knows about is a panel that was deleted.
	/// </summary>
	private static void DrawGrip(ImGui gui, ImRect strip)
	{
		float width = strip.W * 0.04f;
		float thickness = Mathf.Max(2f, gui.GetRowHeight() * 0.08f);

		gui.Canvas.Rect(new ImRect(strip.Center.x - width * 0.5f, strip.Y + thickness, width, thickness),
			Color.Style.Text.Muted, thickness * 0.5f);
	}

	/// <summary>
	///     The controls, always laid out at their full height against the underside of the bar, and
	///     clipped to however far the drawer has come out. That is what makes it read as a drawer
	///     rather than as a fade: the buttons arrive from behind the bar with their far edge still
	///     cut off by it.
	///     The clip does the input too. Imui throws away a hover that falls outside the active clip
	///     rect, so a button half out of the slot is live on exactly the half that can be seen.
	/// </summary>
	private void DrawDrawer(ImGui gui, ImRect rect, float full, AthStateMachine run, RunHudView view)
	{
		if (rect.H <= 0f)
		{
			return;
		}

		gui.Canvas.PushClipRect(rect);

		try
		{
			_controls.Draw(gui, Padded(gui, new ImRect(rect.X, rect.Top - full, rect.W, full)), run, view);
		}
		finally
		{
			gui.Canvas.PopClipRect();
		}
	}

	/// <summary>
	///     Open while the pointer is on it, shut once it has gone, and never anywhere but on its
	///     way between the two. Unscaled, so the drawer moves at the same speed in a paused game -
	///     which is exactly where the stop button is wanted.
	/// </summary>
	private float Advance(bool idle)
	{
		if (idle)
		{
			_open = 1f;

			return _open;
		}

		float step = Time.unscaledDeltaTime / SlideSeconds;

		_open = Mathf.Clamp01(_open + (_mouseOverWindow ? step : -step));

		return _open;
	}

	private static ImRect Padded(ImGui gui, ImRect rect)
	{
		float margin = UiMetrics.Margin(gui);

		return rect.WithPadding(margin, margin, margin * 0.5f, margin * 0.5f);
	}

	/// <summary>The timeline is a rule, not a row: it keeps its own height whatever it is given.</summary>
	private static ImRect Hairline(ImRect rect)
	{
		float height = Mathf.Max(4f, rect.H * 0.34f);

		return new ImRect(rect.X, rect.Y + (rect.H - height) * 0.5f, rect.W, height);
	}
}
