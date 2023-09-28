using System;
using AuthorTimeHunting.Commands;
using BepInEx;
using ZeepSDK.ChatCommands;
using ZeepSDK.Messaging;
using ZeepSDK.Multiplayer;

namespace AuthorTimeHunting;

public class ChallengeStateManager
{
    public Challenge Challenge;

    public ChallengeStateManager()
    {
        Challenge = new Challenge(this);
    }

    public void StartChallenge(string arguments)
    {
        if (Challenge.IsChallengeRunning)
        {
            MessengerApi.LogWarning(
                "hunt is already running. '/hunt restart' or '/hunt restart [minutes]' if you wanna try again.");
            return;
        }

        if (!arguments.IsNullOrWhiteSpace())
        {
            var challengeDuration = int.Parse(arguments);
            if (challengeDuration > 24 * 60)
            {
                MessengerApi.LogError($"Challenge exceeds maximum duration of 24h ({24 * 60}min)");
                return;
            }

            if (challengeDuration < 5)
            {
                MessengerApi.LogError("Challenge exceeds minimum duration of 5min");
                return;
            }

            Challenge.ChallengeDurationInMinutes = challengeDuration;
        }

        MultiplayerApi.DisconnectedFromGame += StopChallenge;
        MessengerApi.LogSuccess("hunt successfully started.", 5f);
        Challenge.SwitchState(new StateStarting(Challenge));
        Challenge.IsChallengeRunning = true;
    }

    public void RestartChallenge(string arguments)
    {
        if (Challenge.IsChallengeRunning)
        {
            var duration = Challenge.ChallengeDurationInMinutes.ToString();
            if (!arguments.IsNullOrWhiteSpace())
                duration = arguments;
            Challenge.SwitchState(new StateEnding(Challenge));
            StartChallenge(duration);
        }
    }

    public void StopChallenge()
    {
        if (!Challenge.IsChallengeRunning)
        {
            MessengerApi.LogWarning("hunt is not running.");
            return;
        }

        MultiplayerApi.DisconnectedFromGame -= StopChallenge;
        Challenge.ChallengeState.Exit();
        Challenge = new Challenge(this);
        MessengerApi.LogSuccess("hunt successfully stopped.", 5f);
        Challenge.IsChallengeRunning = false;
    }
}