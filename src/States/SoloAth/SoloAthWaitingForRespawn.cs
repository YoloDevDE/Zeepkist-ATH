using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using FMODSyntax;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthWaitingForRespawn : IState
{
    public async void Enter()
    {
        SpeechBubble.Custom(GetType().Name, Color.blue);
        ChatApi.SendMessage("AT gained - Respawn to continue");
        AudioEvents.FoundGift.Play();
    }

    public void Exit() { }
    public void Update() { }
}