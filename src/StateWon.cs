using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class StateWon : ChallengeState
{
    private readonly Challenge _challenge;


    public StateWon(Challenge challenge) : base(challenge)
    {
        _challenge = challenge;
    }

    public override void Enter()
    {
        _challenge.Authortimes.Add(PlayerManager.Instance.currentMaster.authorTime);
        RacingApi.PlayerSpawned += TransferTo;
    }

    public override void Exit()
    {
        RacingApi.PlayerSpawned -= TransferTo;
    }

    public void TransferTo()
    {
        _challenge.SkipLevel("Skipping to next...");
        _challenge.SwitchState(new StateLoading(_challenge));
    }
}