using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.SoloAth;

public class SoloAthLevelLoading : IState
{
    public void Enter()
    {
        ChatApi.SendMessage(GetType().Name);
    }

    public void Exit() { }
    public void Update() { }
}