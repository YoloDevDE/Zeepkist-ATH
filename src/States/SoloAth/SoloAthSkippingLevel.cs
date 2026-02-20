using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthSkippingLevel : IState
{
    public async void Enter()
    {
        SpeechBubble.Custom(GetType().Name, Color.blue);
        await PlaylistService.Instance.QueueNextRandomLevel();
        PlaylistService.Instance.SkipLevel();
        ChatApi.SendMessage("Skipping Level...");
    }

    public void Exit() { }
    public void Update() { }
}