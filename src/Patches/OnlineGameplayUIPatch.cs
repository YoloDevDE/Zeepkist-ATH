using System.Reflection;
using AuthorTimeHunting.States.Ath.States;
using HarmonyLib;
using TMPro;
using UnityEngine;

namespace AuthorTimeHunting.Patches;

/// <summary>
///     Keeps RoundOverText visible only during the ATH countdown.
///     During GameState 0, the game calls EndRoundBuffer.SetActive(false) every frame,
///     which hides RoundOverText (its child). During GameState 1, the alpha is animated
///     from 0. This patch overrides both behaviours only while the countdown is running.
/// </summary>
[HarmonyPatch(typeof(OnlineGameplayUI), "Update")]
public static class OnlineGameplayUIPatch
{
    private static FieldInfo _endRoundBufferField;
    private static FieldInfo _roundOverTextField;

    [HarmonyPostfix]
    public static void Postfix(OnlineGameplayUI __instance)
    {
        if (!StateAthStarting.IsCountdownActive)
        {
            return;
        }

        // Lazily resolve private fields via reflection
        if (_endRoundBufferField == null)
        {
            _endRoundBufferField = typeof(OnlineGameplayUI).GetField("EndRoundBuffer", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        if (_roundOverTextField == null)
        {
            _roundOverTextField = typeof(OnlineGameplayUI).GetField("RoundOverText", BindingFlags.NonPublic | BindingFlags.Instance);
        }

        // Force the EndRoundBuffer container active so RoundOverText is visible
        GameObject endRoundBuffer = _endRoundBufferField?.GetValue(__instance) as GameObject;

        if (endRoundBuffer != null && !endRoundBuffer.activeSelf)
        {
            endRoundBuffer.SetActive(true);
        }

        // Keep RoundOverText alpha at 1 (GameState 1 animates it from 0)
        TMP_Text roundOverText = _roundOverTextField?.GetValue(__instance) as TMP_Text;

        if (roundOverText != null)
        {
            Color c = roundOverText.color;

            if (c.a < 1f)
            {
                roundOverText.color = new Color(c.r, c.g, c.b, 1f);
            }
        }
    }
}