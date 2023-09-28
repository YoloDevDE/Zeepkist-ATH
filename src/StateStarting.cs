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
            .AddLine("Commands:")
            .AddLine("/hunt skip")
            .AddLine("/hunt broken")
            .AddLine("/hunt stop")
            .AddLine("/hunt restart or /hunt restart [minutes]")
            .AddSeparator()
            .AddLine("DO NOT USE /skip /fs /forceskip DURING THE CHALLENGE!!")
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