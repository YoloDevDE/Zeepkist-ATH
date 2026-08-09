using System;
using System.Collections.Generic;
using AuthorTimeHunting.Gamemodes;
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
	private const string _title = "AUTHOR TIME HUNTING";

	private const float _titleSize = 1.8f;
	private const float _messageSize = 1.05f;

	private const float _lineHeight = 1.9f;
	private const float _titleLines = 1.4f;

	/// <summary>The air between the rules of the run and the list of what the mod is doing about it.</summary>
	private const float _stepsGap = 1.2f;

	/// <summary>How much of a line the box in front of a step takes.</summary>
	private const float _markSize = 0.7f;

	private const float _thumbnailWidthFraction = 0.34f;
	private const float _thumbnailMaxWidth = 640f;
	private const float _thumbnailAspect = 9f / 16f;

	private const float _settingsWidthFraction = 0.3f;
	private const float _settingsMaxWidth = 520f;

	private const float _dotsPerSecond = 2f;

	/// <summary>
	///     Nothing the mod waits for takes half a minute, and the clock is put back to the top by
	///     every step that finishes - so this is thirty seconds of no progress at all, not thirty
	///     seconds of setup. Past that something has gone wrong that nobody wrote a handler for,
	///     and a screen covering the whole game is the worst possible thing to leave behind.
	/// </summary>
	private const float _maxSeconds = 30f;

	/// <summary>
	///     How long the podium runs for. It is the one wait in the whole setup with a fixed length,
	///     which is what makes it the only one worth counting down instead of dotting at. The number
	///     is the podium's and nothing depends on it being exact - the screen goes away when the
	///     game starts loading, whatever this says at the time.
	/// </summary>
	private const float _podiumSeconds = 8f;

	/// <summary>
	///     Fully opaque, unlike <see cref="ColorExtensions.SurfaceColors.Backdrop" />: the menus
	///     behind this one are clickable, and a player who can see them will click them.
	/// </summary>
	private static readonly Color32 _backdrop = new(8, 9, 12, 255);

	private static readonly string _spacedTitle = UiScreen.Spaced(_title);

	/// <summary>The four dot counts the animation cycles through, so no draw builds a string.</summary>
	private static readonly string[] _dotSteps = ["", ".", "..", "..."];

	private readonly OverlayLayer _layer = new();

	/// <summary>What the mod has done so far, oldest first. The last one is the one it is doing.</summary>
	private readonly List<SetupStep> _steps = [];

	private readonly AthImage _thumbnail;

	private float _countdownEnd;

	private float _hideAt;

	private SetupLine[] _lines = [];

	/// <summary>
	///     Set once the game has started loading the level the hunt will be played on. Only used to
	///     say so on the checklist - the screen stays up over the game's own loading screen until
	///     the hunt has the level, which is the whole point of it.
	/// </summary>
	private bool _loading;

	/// <summary>The level the lobby is about to load, once there is one to name.</summary>
	private NextLevelView _next;

	/// <summary>
	///     Set when the round the lobby was opened with ends, which is the podium starting. From
	///     here the mod knows which level is coming and the screen stops describing the run about to
	///     start and starts describing the level about to load.
	/// </summary>
	private bool _podium;

	private string _tagline = "";

	public LoadingOverlay(AthImage thumbnail)
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
	public AthController ActiveRun { get; set; }

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
	///     Three ways this screen ends: the hunt got its level, the player asked for it to go, or
	///     nothing has happened for long enough that nothing is going to.
	///     It used to stand down the moment the game loaded a scene, on the reasoning that the
	///     game's own loading screen had taken over and the mod should get out of the way. What the
	///     player got out of that was the lobby, the round, the podium and the level load with
	///     nothing over them - the mod let go at the first of four scene loads and left the player
	///     looking at a lobby they had no business in for the length of a podium. So the screen now
	///     holds until the hunt has a level under the wheels, which is the one moment there is
	///     something else worth looking at.
	///     Holding it needs the last two exits to be real, because nothing behind this screen can be
	///     reached while it is up: a setup that gets stuck must still hand the game back.
	/// </summary>
	private bool Done()
	{
		if (ActiveRun?.Ctx.CurrentLevel != null)
		{
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

		Logger.LogWarning($"LoadingOverlay: '{Message}' has been up for {_maxSeconds:0} seconds. Standing down.");

		return true;
	}

	/// <summary>
	///     The scene load after the podium is the level itself starting to load. It is the last
	///     entry on the checklist and, just as importantly, the last thing that puts the watchdog
	///     back to the top: a level coming off the workshop can take longer than the wait before it.
	/// </summary>
	private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
	{
		if (!Visible || !_podium || _loading)
		{
			return;
		}

		_loading = true;
		Step("Loading the level");
	}

	/// <summary>
	///     The round ending is the podium starting, and the podium is the last thing between the
	///     player and their first level. That is the moment the screen stops saying "wait" and
	///     starts saying how long for - and the moment the lobby's playlist has settled on which
	///     level that is, so it is also where the screen starts showing it.
	/// </summary>
	private void OnRoundEnded()
	{
		if (!Visible)
		{
			return;
		}

		_podium = true;
		Step("Starting in");
		CountdownTo(_podiumSeconds);
	}

	public void Show(string message)
	{
		Message = message;
		_podium = false;
		_loading = false;
		_next = null;
		_lines = [];
		_steps.Clear();
		_tagline = "";
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + _maxSeconds;
		Visible = true;
		_layer.Raise();
		Logger.LogInfo($"LoadingOverlay: {message}");
	}

	/// <summary>
	///     Ticks off whatever the mod was doing and says what it is doing now. The player is looking
	///     at a screen that is hiding a lobby being built for them; a list that grows is the
	///     difference between that and a screen that has hung.
	///     A step never puts the screen back up. It used to, which meant that dismissing it with
	///     Escape only lasted until the setup reached its next step and threw it over the game
	///     again.
	/// </summary>
	public void Step(string label)
	{
		if (!Visible)
		{
			return;
		}

		Finish();

		_steps.Add(new SetupStep(label));
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + _maxSeconds;

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
		_next = null;
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

	/// <summary>
	///     Once the podium is running, the screen has something better to say than the rules of the
	///     run: which level is coming. It takes over the same three slots the rules were in - the
	///     picture, the line under the title and the block of rows - because a player who has read
	///     the rules through a whole lobby setup has read them, and the level is the thing they are
	///     actually waiting for.
	///     Resolved once and kept. The lobby only settles on its next level around the time the
	///     podium starts, so this is asked every frame until it answers and never again.
	/// </summary>
	private void Preview()
	{
		if (!_podium || _next != null)
		{
			return;
		}

		NextLevelView next = NextLevelView.From(ActiveRun);

		if (next == null)
		{
			return;
		}

		_next = next;
		_tagline = next.Name;
		_lines =
		[
			new SetupLine("Author", next.Author),
			new SetupLine("Author Time", next.AuthorTime),
			new SetupLine("Gold Time", next.GoldTime)
		];
	}

	private void Draw(ImGui gui)
	{
		Preview();

		ImRect screen = gui.Canvas.ScreenRect;

		gui.Canvas.Rect(screen, _backdrop);

		float size = gui.Style.Layout.TextSize;
		float line = size * _lineHeight;

		float picture = Mathf.Min(screen.W * _thumbnailWidthFraction, _thumbnailMaxWidth) * _thumbnailAspect;
		float tagline = _lines.Length == 0 ? 0f : line;
		float rows = _lines.Length * line;
		float steps = Mathf.Max(1, _steps.Count) * line;
		float height = picture + line * (_titleLines + _stepsGap) + tagline + rows + steps;

		float top = screen.Y + (screen.H + height) * 0.5f;
		float underPicture = top - picture;
		float underTitle = underPicture - line * _titleLines;
		float underTagline = underTitle - tagline;
		float underRows = underTagline - rows;

		DrawThumbnail(gui, new ImRect(screen.X, underPicture, screen.W, picture));

		gui.Canvas.Text(_spacedTitle.AsSpan(), Color.Zeepkist.Medal.Author,
			new ImRect(screen.X, underTitle, screen.W, line * _titleLines), size * _titleSize);

		gui.Canvas.Text(_tagline.AsSpan(), Color.Style.Surface.White,
			new ImRect(screen.X, underTagline, screen.W, tagline), size * _messageSize);

		DrawSettings(gui, screen, underTagline, line, size);
		DrawSteps(gui, screen, underRows - line * _stepsGap, line, size);
	}

	private void DrawSteps(ImGui gui, ImRect screen, float top, float line, float size)
	{
		if (_steps.Count == 0)
		{
			gui.Canvas.Text((Message + Dots()).AsSpan(), Color.Style.Text.Muted,
				new ImRect(screen.X, top - line, screen.W, line), size * _messageSize);

			return;
		}

		float width = Mathf.Min(screen.W * _settingsWidthFraction, _settingsMaxWidth);
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
		float box = size * _markSize;
		ImRect mark = new(row.X, row.Y + (row.H - box) * 0.5f, box, box);

		gui.Canvas.Rect(mark, step.Done ? Color.Style.Status.Positive : Color.Style.Surface.Track, box * 0.25f);

		ImRect text = new(row.X + box * 2f, row.Y, row.W - box * 2f, row.H);

		gui.Canvas.Text(StepLabel(step, last).AsSpan(),
			step.Done ? Color.Style.Text.Muted : Color.Style.Surface.White, text, size * _messageSize, 0f);
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
		float width = Mathf.Min(screen.W * _settingsWidthFraction, _settingsMaxWidth);
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
		Texture2D picture = Picture();

		if (picture == null)
		{
			return;
		}

		gui.Image(picture, rect, true);
	}

	/// <summary>
	///     The level's own picture once the game has loaded one, the mod's logo until then. The
	///     thumbnail arrives a frame or two after it is first asked for, and the logo in its place
	///     keeps the screen from being a hole for those frames.
	/// </summary>
	private Texture2D Picture()
	{
		Texture2D level = _next == null ? null : LevelThumbnails.Get(_next.LevelUid);

		return level == null ? _thumbnail.Texture : level;
	}

	private static string Dots()
	{
		return _dotSteps[(int)(Time.unscaledTime * _dotsPerSecond) % _dotSteps.Length];
	}
}
