using System;
using System.Collections.Generic;
using Imui.Controls;
using Imui.Core;
using UnityEngine;
using ZeepSDK.UI;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The centre-screen overlay: countdown, claimed medals, what is loading, plus short
///     notifications stacked in a corner.
///     All of this used to be written into the game's own RoundOverText, which the game
///     fights over - it deactivates the containing panel every frame during a race and
///     animates the alpha up from zero when a round ends. Keeping ATH's text alive there took
///     a Harmony patch on OnlineGameplayUI.Update plus reflection to reach a private field,
///     re-applying the text every frame. Drawing it ourselves removes all of that: the mod no
///     longer patches the game at all.
///     Unlike the panels this draws straight onto the canvas with no window chrome, because a
///     banner is not something you drag around. That also means nothing clips it for us, so
///     every size here is measured off the current screen rather than assumed.
/// </summary>
public class AthOverlay : IZeepGUIDrawer
{
	/// <summary>
	///     Distance from the top of the screen, as a share of its height. Kept clear of the
	///     HUD, which owns the top of the screen while a hunt is running.
	/// </summary>
	private const float BannerTopFraction = 0.32f;

	private const float BannerWidthFraction = 0.45f;
	private const float BannerMinWidth = 360f;
	private const float BannerMaxWidth = 760f;

	/// <summary>The headline is drawn this much larger than body text - it is a banner, not a line.</summary>
	private const float HeadlineScale = 1.5f;

	private const float ToastWidthFraction = 0.24f;
	private const float ToastMinWidth = 260f;
	private const float ToastMaxWidth = 420f;

	/// <summary>
	///     Older notifications are dropped past this. The stack is capped again at draw time
	///     against the actual screen height; this is just so the list cannot grow unbounded.
	/// </summary>
	private const int MaxToasts = 8;

	private const float FadeInSeconds = 0.12f;

	private readonly List<Toast> _toasts = new();
	private OverlayBanner _banner;
	private float _bannerShownAt;

	public void OnZeepGUI(ImGui gui)
	{
		try
		{
			DrawBanner(gui);
			DrawToasts(gui);
		}
		catch (Exception e)
		{
			// Shared GUI pass: a drawer that throws would throw every frame.
			Logger.LogError($"AthOverlay: Draw failed, clearing it: {e.Message}\n{e.StackTrace}");
			_banner = null;
			_toasts.Clear();
		}
	}

	/// <summary>Replaces whatever is on screen. Null clears it.</summary>
	public void ShowBanner(OverlayBanner banner)
	{
		_banner = banner;
		_bannerShownAt = Time.unscaledTime;
	}

	public void ClearBanner()
	{
		_banner = null;
	}

	/// <summary>Adds a short notification to the corner stack.</summary>
	public void Notify(string text, Color32 colour, float seconds = 5f)
	{
		if (string.IsNullOrEmpty(text))
		{
			return;
		}

		_toasts.Add(new Toast(text, colour, Time.unscaledTime + seconds));

		if (_toasts.Count > MaxToasts)
		{
			_toasts.RemoveRange(0, _toasts.Count - MaxToasts);
		}

		Logger.LogInfo($"AthOverlay: {text}");
	}

	public void Clear()
	{
		_banner = null;
		_toasts.Clear();
	}

	private void DrawBanner(ImGui gui)
	{
		OverlayBanner banner = _banner;

		if (banner == null)
		{
			return;
		}

		float age = Time.unscaledTime - _bannerShownAt;

		if (banner.DisplaySeconds > 0f && age > banner.DisplaySeconds)
		{
			_banner = null;
			return;
		}

		// The original medal text faded in over a moment; keep that, it stops the banner
		// from snapping into view mid-corner-entry.
		float alpha = FadeInSeconds <= 0f ? 1f : Mathf.Clamp01(age / FadeInSeconds);

		ImRect screen = gui.Canvas.SafeScreenRect;
		float bodySize = gui.Style.Layout.TextSize;
		float rowHeight = gui.GetRowHeight();
		float headlineHeight = rowHeight * HeadlineScale;

		float width = UiMetrics.Width(gui, BannerWidthFraction, BannerMinWidth, BannerMaxWidth);
		int lines = banner.Lines?.Count ?? 0;
		float height = Mathf.Min(headlineHeight + lines * rowHeight, screen.H);

		ImRect area = new(screen.Left + (screen.W - width) * 0.5f,
			screen.Top - screen.H * BannerTopFraction - height,
			width,
			height);

		ImRect line = area.TakeTop(headlineHeight, out ImRect rest);
		UiText.Centre(gui, banner.Headline, Fade(HudPalette.Author, alpha), line, bodySize * HeadlineScale);

		if (banner.Lines == null)
		{
			return;
		}

		foreach (OverlayLine overlayLine in banner.Lines)
		{
			line = rest.TakeTop(rowHeight, out rest);
			UiText.Centre(gui, overlayLine.Text, Fade(overlayLine.Colour, alpha), line, bodySize);
		}
	}

	private void DrawToasts(ImGui gui)
	{
		if (_toasts.Count == 0)
		{
			return;
		}

		float now = Time.unscaledTime;
		_toasts.RemoveAll(toast => toast.ExpiresAt <= now);

		ImRect screen = gui.Canvas.SafeScreenRect;
		float rowHeight = gui.GetRowHeight();
		float margin = UiMetrics.Margin(gui);
		float width = UiMetrics.Width(gui, ToastWidthFraction, ToastMinWidth, ToastMaxWidth);

		// Never let the stack climb past the screen: on a short canvas the oldest ones go.
		int visible = Mathf.Clamp(Mathf.FloorToInt((screen.H - margin * 2f) / rowHeight), 1, _toasts.Count);
		int first = _toasts.Count - visible;
		float y = screen.Bottom + margin;

		// Oldest at the bottom, so a new one appears above rather than shoving the rest.
		for (int i = 0; i < visible; i++)
		{
			Toast toast = _toasts[first + i];
			ImRect rect = new(screen.Left + margin, y + i * rowHeight, width, rowHeight);
			UiText.Left(gui, toast.Text, toast.Colour, rect);
		}
	}

	private static Color32 Fade(Color32 colour, float alpha)
	{
		return new Color32(colour.r, colour.g, colour.b, (byte)(255 * Mathf.Clamp01(alpha)));
	}

	private readonly struct Toast
	{
		public Toast(string text, Color32 colour, float expiresAt)
		{
			Text = text;
			Colour = colour;
			ExpiresAt = expiresAt;
		}

		public string Text { get; }
		public Color32 Colour { get; }
		public float ExpiresAt { get; }
	}
}