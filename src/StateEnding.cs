using ZeepSDK.Chat;

namespace AuthorTimeHunting;

public class StateEnding : ChallengeState
{
    private readonly Challenge _challenge;

    public StateEnding(Challenge challenge) : base(challenge)
    {
        _challenge = challenge;
    }

    public override void Enter()
    {
        _challenge.ChallengeStateManager.StopChallenge();
    }

    public override void Exit()
    {
        ChatApi.SendMessage("/servermessage remove");
        _challenge.Authortimes.Add(PlayerManager.Instance.currentMaster.authorTime);
        _challenge.endStats();
        _challenge.IsChallengeRunning = false;
    }
}