using System;
using AuthorTimeHunting.Entities;
using Crosstales;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Manages the ATH medal-text overlay shown on RoundOverText.
///     Call <see cref="SetMedalText" /> to queue a message; the patch writes it
///     every frame while active so it survives the game's own text resets.
///     Call <see cref="ClearMedalText" /> on round-start to hide it again.
/// </summary>
public static class MedalTextHelper
{
	private const string AthHeader =
		"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>";

	private const float FadeInDuration = 0.12f;
	private static float _shownAt;
	private static bool _hasOriginalAlignment;
	private static TextAlignmentOptions _originalAlignment;

	/// <summary>True while a medal message should be kept visible on RoundOverText.</summary>
	public static bool IsMedalTextActive { get; private set; }

	/// <summary>The full formatted string currently queued for display.</summary>
	public static string PendingText { get; private set; }

	/// <summary>
	///     Queues <paramref name="text" /> for display on RoundOverText with the ATH header.
	///     The text is written every Update frame by <c>OnlineGameplayUIPatch</c> until cleared.
	/// </summary>
	public static void SetMedalText(string text)
	{
		PendingText = $"<align=left><margin-left=45%>{AthHeader}{text}</align>";
		IsMedalTextActive = true;
		_shownAt = Time.unscaledTime;
	}

	public static void SetMedalProgressText(Level level, double runTime, bool isNewMedal)
	{
		if (level == null)
		{
			return;
		}

		Level.LevelStatus currentMedal = ResolveRunMedal(level, runTime);

		if (currentMedal is not (Level.LevelStatus.AUTHOR or Level.LevelStatus.GOLD))
		{
			return;
		}

		string medalName = currentMedal == Level.LevelStatus.AUTHOR ? "AUTHOR" : "GOLD";
		string medalColorHex = currentMedal == Level.LevelStatus.AUTHOR
			? ColorDefinitions.Author.CTToHexRGB()
			: ColorDefinitions.Gold.CTToHexRGB();
		string stateColorHex = isNewMedal ? "50E451" : "D5D5D5";
		string statePrefix = isNewMedal ? "NEW" : "CURRENT";

		string titleLine =
			$"<#{stateColorHex}><b>{statePrefix}</b></color> <#DCDCDC>medal:</color> <b><#{medalColorHex}>{medalName}</color></b>";
		string nextMedalLine = BuildNextMedalLine(level, runTime, currentMedal);

		SetMedalText($"{titleLine}<br>{nextMedalLine}");
	}

	/// <summary>Clears the active medal text (call on round-start).</summary>
	public static void ClearMedalText()
	{
		IsMedalTextActive = false;
		PendingText = null;
		_shownAt = 0f;
	}

	public static float GetCurrentAlpha()
	{
		if (!IsMedalTextActive)
		{
			return 0f;
		}

		float elapsed = Time.unscaledTime - _shownAt;

		if (elapsed < 0f)
		{
			return 0f;
		}

		if (elapsed <= FadeInDuration)
		{
			return Mathf.Clamp01(elapsed / FadeInDuration);
		}

		return 1f;
	}

	/// <summary>
	///     Writes <see cref="PendingText" /> into the provided <paramref name="roundOverText" /> component.
	///     Called by <c>OnlineGameplayUIPatch</c> each frame while <see cref="IsMedalTextActive" /> is true.
	/// </summary>
	public static void ApplyToUI(TMP_Text roundOverText)
	{
		if (roundOverText == null || !IsMedalTextActive || PendingText == null)
		{
			return;
		}

		float alpha = GetCurrentAlpha();

		if (alpha <= 0f)
		{
			return;
		}

		roundOverText.SetText(PendingText);

		if (!_hasOriginalAlignment)
		{
			_originalAlignment = roundOverText.alignment;
			_hasOriginalAlignment = true;
		}

		if (roundOverText.alignment != TextAlignmentOptions.Left)
		{
			roundOverText.alignment = TextAlignmentOptions.Left;
		}

		Color currentColor = roundOverText.color;
		roundOverText.color = new Color(currentColor.r, currentColor.g, currentColor.b, alpha);
	}

	public static void ResetUI(TMP_Text roundOverText)
	{
		if (roundOverText == null)
		{
			return;
		}

		if (_hasOriginalAlignment && roundOverText.alignment != _originalAlignment)
		{
			roundOverText.alignment = _originalAlignment;
		}

		_hasOriginalAlignment = false;
	}

	private static Level.LevelStatus ResolveRunMedal(Level level, double runTime)
	{
		if (runTime <= level.AuthorTime)
		{
			return Level.LevelStatus.AUTHOR;
		}

		if (runTime <= level.GoldTime)
		{
			return Level.LevelStatus.GOLD;
		}

		return Level.LevelStatus.UNKNOWN;
	}

	private static string BuildNextMedalLine(Level level, double runTime, Level.LevelStatus currentMedal)
	{
		string atTime = level.AuthorTime.GetFormattedTime();
		string atDisplay = $" {atTime}";
		double diffToAuthor = runTime - level.AuthorTime;
		double absDiff = Math.Abs(diffToAuthor);
		string diffSign = StringUtils.GetSign(diffToAuthor);
		string diffColorHex = diffToAuthor <= 0
			? ColorDefinitions.GreenSplit.CTToHexRGB()
			: ColorDefinitions.YellowSplit.CTToHexRGB();
		string diffDisplay = $"{diffSign}{absDiff.GetFormattedTime()}";
		string atLabel = "AT:".PadRight(5);
		string youLabel = "YOU:".PadRight(5);

		string atYouBlock =
			$"<mspace=0.58em><#AAAAAA>{atLabel}</color><#{ColorDefinitions.Author.CTToHexRGB()}>{atDisplay}</color><br><#AAAAAA>{youLabel}</color><#{diffColorHex}>{diffDisplay}</color></mspace>";

		if (currentMedal == Level.LevelStatus.AUTHOR)
		{
			return $"{atYouBlock}<br><#A7A7A7>(respawn to skip)</color>";
		}

		return atYouBlock;
	}
}