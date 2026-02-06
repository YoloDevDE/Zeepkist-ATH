using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthStarting : IState
{
    public void Enter()
    {
        SpeechBubble.Success("Solo Ath Started!");
        ChatApi.SendMessage(GetType().Name);
    }

    public void Exit() { }
    public void Update() { }
}