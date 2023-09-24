using ZeepSDK.Chat;
using ZeepSDK.Messaging;

namespace AuthorTimeHunting;

public class ChallengeStatePending : ChallengeState
{
    public override void Enter(Challenge challenge)
    {
        ChatApi.SendMessage("ASdfgh");
    }

    public override void Transition(Challenge challenge)
    {
    }


    public override void OnLevelLoaded(Challenge challenge)
    {
    }
}