using AuthorTimeHunting.Commands;
using ZeepSDK.Chat;

namespace AuthorTimeHunting;

public class StateEnding : ChallengeState
{
    public StateEnding(Challenge challenge) : base(challenge)
    {
    }

    public override void Enter()
    {
        ChatApi.SendMessage("/servermessage remove");
        base.Challenge.SwitchState(new StateStandby(base.Challenge));
    }

    public override void Exit()
    {
        
    }
}