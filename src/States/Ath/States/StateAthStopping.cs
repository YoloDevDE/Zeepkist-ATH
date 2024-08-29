using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Chat;
using ZeepSDK.Playlist;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStopping : IState
{
    public StateAthStopping(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        AthStateMachine.StopTimer();
        try
        {
            ChatApi.SendMessage(AthStateMachine.Ctx.MessageFinalResult());
            ChatApi.SendMessage("/servermessage blue 0 ATH finished!");
            PlaylistApi.CreatePlaylist($"ATH-RUN-{DateTime.Now.ToShortDateString()}T{DateTime.Now.ToShortTimeString()}");
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }


    public void Exit()
    {
    }
}