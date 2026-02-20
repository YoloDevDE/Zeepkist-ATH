using System;
using AuthorTimeHunting.Enums;
using ZeepkistClient;
using ZeepSDK.Level;

namespace AuthorTimeHunting.Guards;

public static class GameGuards
{
    /// <summary>
    ///     True if the local player's current run time has reached/exceeded the current level's author time.
    ///     Safe-guarded against missing data.
    /// </summary>
    public static bool AuthorTimeReached()
    {
        // LevelApi.CurrentLevel can be null while loading.
        if (!LevelApi.CurrentLevel)
        {
            return false;
        }

        // Author time is often stored as seconds (float). Your earlier code compares it that way.
        float authorTime = LevelApi.CurrentLevel.TimeAuthor;
        if (authorTime <= 0f)
        {
            return false;
        }

        // LocalPlayer or CurrentResult can be null depending on game state / initialization.
        float currentTime = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1f;
        if (currentTime < 0f)
        {
            return false;
        }

        // Reached or exceeded => trigger respawn/pause state.
        return currentTime <= authorTime;
    }

    /// <summary>
    ///     Helper to build reusable "threshold reached" guards for other modes/rules.
    ///     Example: When(GameGuards.TimeReached(() => x, () => y))
    /// </summary>
    public static Func<bool> TimeReached(Func<float> currentSeconds, Func<float> targetSeconds)
    {
        if (currentSeconds == null)
        {
            throw new ArgumentNullException(nameof(currentSeconds));
        }

        if (targetSeconds == null)
        {
            throw new ArgumentNullException(nameof(targetSeconds));
        }

        return () =>
        {
            float target = targetSeconds();
            if (target <= 0f)
            {
                return false;
            }

            float current = currentSeconds();
            if (current < 0f)
            {
                return false;
            }

            return current >= target;
        };
    }

    public static bool IsLobbyIn(ZeepkistLobbyState state) => ZeepkistNetwork.CurrentLobby != null
                                                              && ZeepkistNetwork.CurrentLobby.GameState == (int)state;
}