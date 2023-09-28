using System;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class StateStarting : ChallengeState
{
    private readonly Challenge _challenge;

    public StateStarting(Challenge challenge) : base(challenge)
    {
        _challenge = challenge;
    }

    public override void Enter()
    {

        ChatApi.SendMessage("/settime 3600");
        new MessageBuilder()
            .ClearChat()
            .AddLine("Author Time Hunting started")
            .AddLine("Good Luck Have Fun! :smile:")
            .AddSeparator()
            .AddKeyValue("Free Skips", $"{_challenge.FreeSkips}")
            .AddKeyValue("Duration", $"{_challenge.ChallengeDurationInMinutes} Minutes")
            .AddSeparator()
            .BuildAndSend();
        ChatApi.SendMessage("/fs");
        _challenge.StartTime = DateTime.Now;
        _challenge.EndTime = DateTime.Now.AddMinutes(_challenge.ChallengeDurationInMinutes);
        _challenge.SwitchState(new StateLoading(_challenge));
    }

    public override void Exit()
    {
    }

    public void TransferTo()
    {
    }
}