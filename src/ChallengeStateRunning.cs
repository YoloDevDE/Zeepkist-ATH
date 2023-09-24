using System;
using ZeepSDK.Messaging;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class ChallengeStateRunning : ChallengeState
{
    public override void Enter(Challenge challenge)
    {
        RacingApi.CrossedFinishLine += challenge.CheckFinish;
    }

    public override void Transition(Challenge challenge)
    {
        Console.WriteLine("Running Transition");
        RacingApi.CrossedFinishLine -= challenge.CheckFinish;
        challenge.SwitchState(challenge.ChallengeStatePending);
    }

    public override void OnLevelLoaded(Challenge challenge)
    {
    }
}