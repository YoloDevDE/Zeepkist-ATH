using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthEnding : IState
{
    public void Enter()
    {
        SpeechBubble.Custom(GetType().Name, Color.blue);
        ChatApi.SendMessage("Solo Ath Ended!");
    }

    public void Exit() { }
    public void Update() { }
}