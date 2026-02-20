using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using UnityEngine;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthWaitingForFirstLevel : IState
{
    public void Enter()
    {
        SpeechBubble.Custom(GetType().Name, Color.blue);
    }

    public void Exit() { }
    public void Update() { }
}