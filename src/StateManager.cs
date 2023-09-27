using BepInEx;
using ZeepSDK.Messaging;

namespace AuthorTimeHunting;

public class StateManager
{
    private Challenge _challenge = new();
    private bool _isChallengeRunning;

    public void StartChallenge(string arguments)
    {
        if (_isChallengeRunning)
        {
            MessengerApi.LogWarning(
                "ATH is already running. '/ath restart' or '/ath restart [minutes]' if you wanna try again.");
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
                MessengerApi.LogError("Challenge exceeds minimum duration of 24h (5min)");
                return;
            }

            _challenge.ChallengeDurationInMinutes = challengeDuration;
        }


        MessengerApi.LogSuccess("ATH successfully started.", 5f);
        _challenge.SwitchState(new StateStarting(_challenge));
        _isChallengeRunning = true;
    }

    public void RestartChallenge(string arguments)
    {
        //ToDo
    }

    public void StopChallenge()
    {
        if (!_isChallengeRunning)
        {
            MessengerApi.LogWarning("ATH is not running.");
            return;
        }

        _challenge.ChallengeState.Exit();
        _challenge = new Challenge();
        MessengerApi.LogSuccess("ATH successfully stopped.", 5f);
        _isChallengeRunning = false;
    }
}