using System;
using System.IO;
using System.Reflection;
using AuthorTimeHunting.Gamemodes;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

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
public class LoadingOverlay : IZeepGUIDrawer
{
	private const string Title = "AUTHOR TIME HUNTING";

	private const string ThumbnailResource = "AuthorTimeHunting.Thumbnail.png";

	private const float TitleSize = 1.8f;
	private const float MessageSize = 1.05f;

	private const float LineHeight = 1.9f;
	private const float TitleLines = 1.4f;
	private const float StatusLines = 1.4f;

	private const float ThumbnailWidthFraction = 0.34f;
	private const float ThumbnailMaxWidth = 640f;
	private const float ThumbnailAspect = 9f / 16f;

	private const float SettingsWidthFraction = 0.3f;
	private const float SettingsMaxWidth = 520f;

	private const float DotsPerSecond = 2f;
	private const int MaxDots = 4;

	/// <summary>
	///     Nothing the mod waits for takes two minutes. Past that something has gone wrong that
	///     nobody wrote a handler for, and a screen covering the whole game is the worst possible
	///     thing to leave behind.
	/// </summary>
	private const float MaxSeconds = 120f;

	private static readonly Color32 Backdrop = new(8, 9, 12, 255);

	private float _countdownEnd;

	private float _hideAt;

	private SetupLine[] _lines = [];

	private string _tagline = "";

	private Texture2D _thumbnail;

	private bool _thumbnailTried;

	public string Message { get; private set; } = "";

	public bool Visible { get; private set; }

	/// <summary>
	///     The hunt this screen is waiting for. It goes away the moment that hunt has a level
	///     under the wheels - which is after the game's own loading screen, not before it, so the
	///     player never looks at a lobby they have no business in.
	/// </summary>
	public AthStateMachine ActiveRun { get; set; }

	public void OnZeepGUI(ImGui gui)
	{
		if (!Visible)
		{
			return;
		}

		if (ActiveRun?.Ctx.CurrentLevel != null || Time.unscaledTime > _hideAt)
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

	public void Show(string message)
	{
		Message = message;
		_lines = [];
		_tagline = "";
		_countdownEnd = 0f;
		_hideAt = Time.unscaledTime + MaxSeconds;
		Visible = true;
		Logger.LogInfo($"LoadingOverlay: {message}");
	}

	/// <summary>
	///     The rules of the run and how long is left before it starts. When the countdown runs
	///     out the rules stay up and the line under them becomes the message given here - the
	///     run is being set up behind this screen and the player has something to read until the
	///     first level is there.
	/// </summary>
	public void ShowWelcome(IGamemode gamemode, TimeSpan countdown)
	{
		Show("Loading the first level");

		_tagline = gamemode.Description;
		_lines = Describe(gamemode);
		_countdownEnd = Time.unscaledTime + (float)countdown.TotalSeconds;
	}

	public void Hide()
	{
		Visible = false;
		_lines = [];
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
		float height = picture + line * (TitleLines + StatusLines) + tagline + rows;

		float top = screen.Y + (screen.H + height) * 0.5f;
		float underPicture = top - picture;
		float underTitle = underPicture - line * TitleLines;
		float underTagline = underTitle - tagline;
		float underRows = underTagline - rows;

		DrawThumbnail(gui, new ImRect(screen.X, underPicture, screen.W, picture));

		gui.Canvas.Text(Spaced(Title).AsSpan(), Color.Zeepkist.Medal.Author,
			new ImRect(screen.X, underTitle, screen.W, line * TitleLines), size * TitleSize);

		gui.Canvas.Text(_tagline.AsSpan(), Color.Style.Surface.White,
			new ImRect(screen.X, underTagline, screen.W, tagline), size * MessageSize);

		DrawSettings(gui, screen, underTagline, line, size);

		gui.Canvas.Text(Status().AsSpan(), Color.Style.Text.Muted,
			new ImRect(screen.X, underRows - line * StatusLines, screen.W, line * StatusLines), size * MessageSize);
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
		Texture2D thumbnail = Thumbnail();

		if (thumbnail == null)
		{
			return;
		}

		gui.Image(thumbnail, rect, true);
	}

	private string Status()
	{
		int left = Mathf.CeilToInt(_countdownEnd - Time.unscaledTime);

		if (left > 0)
		{
			return $"Starting in {left}";
		}

		return Message + Dots();
	}

	private Texture2D Thumbnail()
	{
		if (_thumbnailTried)
		{
			return _thumbnail;
		}

		_thumbnailTried = true;
		_thumbnail = Load();

		return _thumbnail;
	}

	private static Texture2D Load()
	{
		try
		{
			using Stream stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(ThumbnailResource);

			if (stream == null)
			{
				Logger.LogWarning($"LoadingOverlay: '{ThumbnailResource}' is not in the plugin.");

				return null;
			}

			byte[] png = new byte[stream.Length];
			stream.Read(png, 0, png.Length);

			Texture2D texture = new(2, 2, TextureFormat.RGBA32, false) { hideFlags = HideFlags.HideAndDontSave };
			texture.LoadImage(png);

			return texture;
		}
		catch (Exception e)
		{
			Logger.LogWarning($"LoadingOverlay: Could not load the thumbnail: {e.Message}");

			return null;
		}
	}

	/// <summary>A space between every letter, which is the only letter spacing Imui offers.</summary>
	private static string Spaced(string text)
	{
		return string.Join(" ", text.ToCharArray());
	}

	private static string Dots()
	{
		return new string('.', (int)(Time.unscaledTime * DotsPerSecond) % MaxDots);
	}
}
