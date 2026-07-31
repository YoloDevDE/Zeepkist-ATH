using System.Reflection;
using AuthorTimeHunting.States.Ath.States;
using AuthorTimeHunting.Util;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.Patches;

/// <summary>
///     Keeps RoundOverText visible during the ATH countdown and while a medal text is active.
///     During GameState 0, the game calls EndRoundBuffer.SetActive(false) every frame,
///     which hides RoundOverText (its child). During GameState 1, the alpha is animated
///     from 0. This patch overrides both behaviours while the countdown or a medal text is active.
///     It also re-applies the queued medal text every frame so gold medal text is not lost
///     when the EndRoundBuffer panel becomes visible after OnCrossedFinishLine.
/// </summary>
[HarmonyPatch(typeof(OnlineGameplayUI), "Update")]
public static class OnlineGameplayUIPatch
{
	private static FieldInfo _endRoundBufferField;

	[HarmonyPostfix]
	public static void Postfix(OnlineGameplayUI __instance)
	{
		bool isCountdownActive = StateAthStarting.IsCountdownActive;
		bool isMedalTextActive = MedalTextHelper.IsMedalTextActive;

		TMP_Text roundOverText = __instance.RoundOverText;

		if (roundOverText == null)
		{
			return;
		}

		if (!isMedalTextActive)
		{
			MedalTextHelper.ResetUI(roundOverText);
		}

		if (!isCountdownActive && !isMedalTextActive)
		{
			return;
		}

		// Lazily resolve private fields via reflection
		if (_endRoundBufferField == null)
		{
			_endRoundBufferField =
				typeof(OnlineGameplayUI).GetField("EndRoundBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
		}

		// Force the EndRoundBuffer container active so RoundOverText is visible
		GameObject endRoundBuffer = _endRoundBufferField?.GetValue(__instance) as GameObject;

		if (endRoundBuffer != null && !endRoundBuffer.activeSelf)
		{
			endRoundBuffer.SetActive(true);
		}

		if (isCountdownActive)
		{
			// Keep alpha at 1 for ATH countdown (GameState 1 animates it from 0)
			Color c = roundOverText.color;

			if (c.a < 1f)
			{
				roundOverText.color = new Color(c.r, c.g, c.b, 1f);
			}
		}

		if (isMedalTextActive)
		{
			// Re-apply medal text every frame and control alpha via helper-side fade profile
			MedalTextHelper.ApplyToUI(roundOverText);
		}
	}
}