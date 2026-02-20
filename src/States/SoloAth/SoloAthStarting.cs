using System.Threading.Tasks;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using FMODSyntax;
using UnityEngine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthStarting : IState
{
    private Camera _camera;
    public Camera MainCamera => _camera = _camera ? _camera : Camera.main;

    public async void Enter()
    {
        SpeechBubble.Custom(GetType().Name, Color.blue);
        SpeechBubble.Success("Solo Ath Started!");
        ChatApi.SendMessage("/timeset 86400");
        ChatApi.SendMessage(GetType().Name);

        await PlaylistService.Instance.StartNewPlaylist();
        AudioEvents.Flute_01.Play(MainCamera.transform);
        await Task.Delay(1000);
        AudioEvents.Flute_01.Play(MainCamera.transform);
        await Task.Delay(1000);
        AudioEvents.Flute_01.Play(MainCamera.transform);
        await Task.Delay(1000);
        AudioEvents.Flute_01.Play(MainCamera.transform);
        await Task.Delay(1000);
        AudioEvents.Flute_01.Play(MainCamera.transform);
        await Task.Delay(1000);
        AudioEvents.Flute_01.Play(MainCamera.transform);
        AudioEvents.Flute_08.Play(MainCamera.transform);
        AudioEvents.Flute_16.Play(MainCamera.transform);
        await PlaylistService.Instance.QueueNextRandomLevel();
        PlaylistService.Instance.SkipLevel();
    }

    public void Exit() { }
    public void Update() { }
}