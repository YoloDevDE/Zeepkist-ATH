using System;
using System.Collections.Generic;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI.Hud;
using AuthorTimeHunting.UI.Toolkit;
using AuthorTimeHunting.UI.Views;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using UnityEngine.SceneManagement;
using ZeepSDK.Racing;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI.Screens;

/// <summary>
///     The screen while the mod is doing something the player must not interrupt: leaving a
///     lobby, reaching the lobby server, opening a new one. Three scene loads and two server
///     round trips happen in there, and the game shows its own menus in between - a room list
///     the player could click, a lobby browser that means nothing right now.
///     So the whole screen is painted over. It is drawn last of all the mod's drawers, which in
///     an immediate mode GUI is the same as being on top, and the mod's own windows are hidden
///     before it comes up.
///     The last phase is the welcome: the fresh lobby needs a moment to settle before a hunt
///     takes it over, and a screen that says nothing for ten seconds is a screen that looks
///     hung. So the wait is spent on the rules of the run about to start - the ones a player
///     would otherwise only learn by losing a run to them - and on a countdown that proves the
///     mod is still there.
///     Imui draws everything in one font, so the hierarchy here is made of the three things
///     that are left: size, letter spacing and colour. The title is spaced out because a line
///     of large letters with air between them reads as a title and not as a sentence.
/// </summary>
public class LoadingOverlay : IZeepGUIDrawer, IDisposable
{
	private const string Title = "AUTHOR TIME HUNTING";

	private const float TitleSize = 1.8f;
	private const float MessageSize = 1.05f;

	private const float LineHeight = 1.9f;
	private const float TitleLines = 1.4f;

	/// <summary>The air between the rules of the run and the list of what the mod is doing about it.</summary>
	private const float StepsGap = 1.2f;

	/// <summary>How much of a line the box in front of a step takes.</summary>
	private const float MarkSize = 0.7f;

	private const float ThumbnailWidthFraction = 0.34f;
	private const float ThumbnailMaxWidth = 640f;
	private const float ThumbnailAspect = 9f / 16f;

	private const float SettingsWidthFraction = 0.3f;
	private const float SettingsMaxWidth = 520f;

	private const float DotsPerSecond = 2f;

	/// <summary>
	///     Nothing the mod waits for takes half a minute, and the clock is put back to the top by
	///     every step that finishes - so this is thirty seconds of no progress at all, not thirty
	///     seconds of setup. Past that something has gone wrong that nobody wrote a handler for,
	///     and a screen covering the whole game is the worst possible thing to leave behind.
	/// </summary>
	private const float MaxSeconds = 30f;

	/// <summary>
	///     How long the podium runs for. It is the one wait in the whole setup with a fixed length,
	///     which is what makes it the only one worth counting down instead of dotting at. The number
	///     is the podium's and nothing depends on it being exact - the screen goes away when the
	///     game starts loading, whatever this says at the time.
	/// </summary>
	private const float PodiumSeconds = 8f;

	/// <summary>
	///     Fully opaque, unlike <see cref="ColorExtensions.SurfaceColors.Backdrop" />: the menus
	///     behind this one are clickable, and a player who can see them will click them.
	/// </summary>
	private static readonly Color32 Backdrop = new(8, 9, 12, 255);

	private static readonly string SpacedTitle = UiScreen.Spaced(Title);

	/// <summary>The four dot counts the animation cycles through, so no draw builds a string.</summary>
	private static readonly string[] DotSteps = ["", ".", "..", "..."];

	private readonly OverlayLayer _layer = new();

	/// <summary>What the mod has done so far, oldest first. The last one is the one it is doing.</summary>
	private readonly List<SetupStep> _steps = [];

	private readonly AthThumbnail _thumbnail;

	private float _countdownEnd;

	/// <summary>
	///     Set when the game reloads its game scene under a running hunt, which is the last line of
	///     GameMaster.DoTheOnlineReset and the exact moment the game's own loading screen takes the
	///     screen. That is the cue to get out of the way: from here the player is meant to see the
	///     game load, and the mod's UI comes back on its own when they spawn.
	///     The scene load is the signal rather than "the level is not ready", which is also true for
	///     the whole of the setup this screen exists to cover, and had it disappearing before the
	///     podium had even finished.
	/// </summary>
	private bool _handedOver;

	private float _hideAt;

	private SetupLine[] _lines = [];

	/// <summary>
	///     Whether the game has been seen without a level loaded since this screen went up. The
	///     screen can be raised while the player is still standing on the track of the lobby they
	///     are about to be taken out of, and "there is a level under the wheels" is only a reason
	///     to stand down once there has been a moment where there was not one.
	/// </summary>
	private bool _offTrack;

	private string _tagline = "";

	public LoadingOverlay(AthThumbnail thumbnail)
	{
		_thumbnail = thumbnail;
		RacingApi.RoundEnded += OnRoundEnded;
		SceneManager.sceneLoaded += OnSceneLoaded;
	}

	public string Message { get; private set; } = "";

	public bool Visible { get; private set; }

	/// <summary>
	///     The hunt this screen is waiting for. It goes away the moment that hunt has a level
	///     under the wheels - which is after the game's own loading screen, not before it, so the
	///     player never looks at a lobby they have no business in.
	/// </summary>
	public AthStateMachine ActiveRun { get; set; }

	public void Dispose()
	{
		RacingApi.RoundEnded -= OnRoundEnded;
		SceneManager.sceneLoaded -= OnSceneLoaded;
		_layer.Drop();
	}

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		if (Done())
		{
			Hide();

			return;
		}

		try
		{
			Draw(gui);
		}
		catch (Exception e)
		{
			Logger.LogError($"LoadingOverlay: Draw failed, hiding it: {e.Message}\n{e.StackTrace}");
			Visible = false;
		}
	}

	/// <summary>
	///     Five ways this screen ends, and it needs all five, because it is painted over the whole
	///     game and nothing behind it can be reached while it is up: the hunt got its level, the
	///     game took the screen for its own loading, the player is standing on a track and so is
	///     plainly not waiting for anything, the player asked for it to go, or nothing has happened
	///     for long enough that nothing is going to.
	///     The last three are the ones added after a setup got stuck behind it. Up to then the
	///     screen only came down when the thing it was waiting for arrived, which is fine until it
	///     does not arrive - and then the mod has taken the game away and nobody can take it back.
	/// </summary>
	private bool Done()
	{
		_offTrack |= !GameStateObserver.IsLevelReady;

		if (ActiveRun?.Ctx.CurrentLevel != null || _handedOver)
		{
			return true;
		}

		if (_offTrack && GameStateObserver.IsLevelReady)
		{
			Logger.LogInfo("LoadingOverlay: A level is loaded and the hunt is not waiting on one. Standing down.");

			return true;
		}

		if (Input.GetKeyDown(KeyCode.Escape))
		{
			Logger.LogInfo("LoadingOverlay: Dismissed with Escape.");

			return true;
		}

		return TimedOut();
	}

	private bool TimedOut()
	{
		if (Time.unscaledTime <= _hideAt)
		{
			return false;
		}

		Logger.LogWarning($"LoadingOverlay: '{Message}' has been up for {MaxSeconds:0} seconds. Standing down.");

		return true;
	}

	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!Visible || ActiveRun == null)
		{
			return;
		}

		_handedOver = true;
	}

	/// <summary>
	///     The round ending is the podium starting, and the podium is the last thing between the
	///     player and their first level. That is the moment the screen stops saying "wait" and
	///     starts saying how long for.
	/// </summary>
	private void OnRoundEnded()
	{
		if (!Visible)
		{
			return;
		}

		Step("Starting in");
		CountdownTo(PodiumSeconds);
	}

	public void Show(string message)
	{
		Message = message;
		_handedOver = false;
		_offTrack = false;
		_lines = [];
		_steps.Clear();
		_tagline = "";
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + MaxSeconds;
		Visible = true;
		_layer.Raise();
		Logger.LogInfo($"LoadingOverlay: {message}");
	}

	/// <summary>
	///     Ticks off whatever the mod was doing and says what it is doing now. The player is looking
	///     at a screen that is hiding a lobby being built for them; a list that grows is the
	///     difference between that and a screen that has hung.
	/// </summary>
	public void Step(string label)
	{
		Finish();

		_steps.Add(new SetupStep(label));
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + MaxSeconds;
		Visible = true;

		Logger.LogInfo($"LoadingOverlay: {label}");
	}

	/// <summary>
	///     The last step counts down instead of waiting. Used once the mod knows how long is left -
	///     the podium runs for a fixed time, and that time is the run about to start.
	/// </summary>
	public void CountdownTo(float seconds)
	{
		_countdownEnd = Time.unscaledTime + seconds;
	}

	public void Finish()
	{
		if (_steps.Count == 0)
		{
			return;
		}

		_steps[_steps.Count - 1].Done = true;
	}

	/// <summary>
	///     The rules of the run about to start, put up beside the checklist rather than instead of
	///     it. Setting a lobby up takes as long as it takes, and the rules are the one thing worth
	///     reading while it does - they are otherwise learned by losing a run to them.
	/// </summary>
	public void ShowWelcome(IGamemode gamemode)
	{
		_tagline = gamemode.Description;
		_lines = Describe(gamemode);
	}

	public void Hide()
	{
		Visible = false;
		_layer.Drop();
		_lines = [];
		_steps.Clear();
		_tagline = "";
	}

	private static SetupLine[] Describe(IGamemode gamemode)
	{
		RunSettings settings = gamemode.CreateSettings();

		return
		[
			new SetupLine("Gamemode", gamemode.DisplayName),
			new SetupLine("You have", TimeFormatter.FormatDuration(settings.DurationMs)),
			new SetupLine("A skip costs", TimeFormatter.FormatDuration(settings.PenaltyTimeMs)),
			new SetupLine("Free skips", settings.FreeSkips.ToString()),
			new SetupLine("Levels", settings.RandomPlaylist ? "drawn from the workshop" : "the lobby playlist")
		];
	}

	private void Draw(ImGui gui)
	{
		ImRect screen = gui.Canvas.ScreenRect;

		gui.Canvas.Rect(screen, Backdrop);

		float size = gui.Style.Layout.TextSize;
		float line = size * LineHeight;

		float picture = Mathf.Min(screen.W * ThumbnailWidthFraction, ThumbnailMaxWidth) * ThumbnailAspect;
		float tagline = _lines.Length == 0 ? 0f : line;
		float rows = _lines.Length * line;
		float steps = Mathf.Max(1, _steps.Count) * line;
		float height = picture + line * (TitleLines + StepsGap) + tagline + rows + steps;

		float top = screen.Y + (screen.H + height) * 0.5f;
		float underPicture = top - picture;
		float underTitle = underPicture - line * TitleLines;
		float underTagline = underTitle - tagline;
		float underRows = underTagline - rows;

		DrawThumbnail(gui, new ImRect(screen.X, underPicture, screen.W, picture));

		gui.Canvas.Text(SpacedTitle.AsSpan(), Color.Zeepkist.Medal.Author,
			new ImRect(screen.X, underTitle, screen.W, line * TitleLines), size * TitleSize);

		gui.Canvas.Text(_tagline.AsSpan(), Color.Style.Surface.White,
			new ImRect(screen.X, underTagline, screen.W, tagline), size * MessageSize);

		DrawSettings(gui, screen, underTagline, line, size);
		DrawSteps(gui, screen, underRows - line * StepsGap, line, size);
	}

	private void DrawSteps(ImGui gui, ImRect screen, float top, float line, float size)
	{
		if (_steps.Count == 0)
		{
			gui.Canvas.Text((Message + Dots()).AsSpan(), Color.Style.Text.Muted,
				new ImRect(screen.X, top - line, screen.W, line), size * MessageSize);

			return;
		}

		float width = Mathf.Min(screen.W * SettingsWidthFraction, SettingsMaxWidth);
		float left = screen.X + (screen.W - width) * 0.5f;

		for (int i = 0; i < _steps.Count; i++)
		{
			DrawStep(gui, new ImRect(left, top - line * (i + 1), width, line), _steps[i], size,
				i == _steps.Count - 1);
		}
	}

	/// <summary>
	///     A box that fills in when the step is done. Imui draws everything in one font and a tick
	///     is a glyph that font may not have, so the mark is drawn rather than written.
	/// </summary>
	private void DrawStep(ImGui gui, ImRect row, SetupStep step, float size, bool last)
	{
		float box = size * MarkSize;
		ImRect mark = new(row.X, row.Y + (row.H - box) * 0.5f, box, box);

		gui.Canvas.Rect(mark, step.Done ? Color.Style.Status.Positive : Color.Style.Surface.Track, box * 0.25f);

		ImRect text = new(row.X + box * 2f, row.Y, row.W - box * 2f, row.H);

		gui.Canvas.Text(StepLabel(step, last).AsSpan(),
			step.Done ? Color.Style.Text.Muted : Color.Style.Surface.White, text, size * MessageSize, 0f);
	}

	/// <summary>
	///     The step being worked on says so, with dots while the wait is open ended and with the
	///     seconds left once the mod knows how many there are.
	/// </summary>
	private string StepLabel(SetupStep step, bool last)
	{
		if (step.Done || !last)
		{
			return step.Label;
		}

		int left = Mathf.CeilToInt(_countdownEnd - Time.unscaledTime);

		if (_countdownEnd > 0f && left > 0)
		{
			return $"{step.Label} {left}";
		}

		return step.Label + Dots();
	}

	private void DrawSettings(ImGui gui, ImRect screen, float top, float line, float size)
	{
		float width = Mathf.Min(screen.W * SettingsWidthFraction, SettingsMaxWidth);
		float left = screen.X + (screen.W - width) * 0.5f;

		for (int i = 0; i < _lines.Length; i++)
		{
			ImRect row = new(left, top - line * (i + 1), width, line);

			gui.Canvas.Text(_lines[i].Label.AsSpan(), Color.Style.Text.Muted, row, size, 0f);
			gui.Canvas.Text(_lines[i].Value.AsSpan(), Color.Style.Text.Default, row, size, 1f);
		}
	}

	private void DrawThumbnail(ImGui gui, ImRect rect)
	{
		Texture2D thumbnail = _thumbnail.Texture;

		if (thumbnail == null)
		{
			return;
		}

		gui.Image(thumbnail, rect, true);
	}

	private static string Dots()
	{
		return DotSteps[(int)(Time.unscaledTime * DotsPerSecond) % DotSteps.Length];
	}
}
