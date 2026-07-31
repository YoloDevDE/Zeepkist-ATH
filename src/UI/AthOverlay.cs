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
///     Unlike AthWindow this draws straight onto the canvas with no window chrome, because a
///     banner is not something you drag around.
/// </summary>
public class AthOverlay : IZeepGUIDrawer
{
	private const float BannerTopMargin = 120f;
	private const float BannerWidth = 520f;
	private const float ToastWidth = 360f;
	private const float ToastMargin = 16f;
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
		float rowHeight = gui.GetRowHeight();
		int rows = 1 + (banner.Lines?.Count ?? 0);

		ImRect area = new(screen.Left + (screen.W - BannerWidth) * 0.5f,
			screen.Top - BannerTopMargin - rows * rowHeight,
			BannerWidth,
			rows * rowHeight);

		ImRect line = area.TakeTop(rowHeight, out ImRect rest);
		Text(gui, banner.Headline, Fade(HudPalette.Author, alpha), line);

		if (banner.Lines == null)
		{
			return;
		}

		foreach (OverlayLine overlayLine in banner.Lines)
		{
			line = rest.TakeTop(rowHeight, out rest);
			Text(gui, overlayLine.Text, Fade(overlayLine.Colour, alpha), line);
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
		float y = screen.Bottom + ToastMargin;

		// Oldest at the bottom, so a new one appears above rather than shoving the rest.
		for (int i = 0; i < _toasts.Count; i++)
		{
			Toast toast = _toasts[i];
			ImRect rect = new(screen.Left + ToastMargin, y + i * rowHeight, ToastWidth, rowHeight);
			Text(gui, toast.Text, toast.Colour, rect);
		}
	}

	private static Color32 Fade(Color32 colour, float alpha)
	{
		return new Color32(colour.r, colour.g, colour.b, (byte)(255 * Mathf.Clamp01(alpha)));
	}

	private static void Text(ImGui gui, string text, Color32 colour, ImRect rect)
	{
		gui.Text(text.AsSpan(), colour, rect);
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
