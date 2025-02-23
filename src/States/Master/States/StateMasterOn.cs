using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Master.States;

public class StateMasterOn : IState
{
    // Constructor
    public StateMasterOn(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        SubStateMachine = new AthStateMachine();
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)SubStateMachine;

    public IStateMachine SubStateMachine { get; }
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
        CommandStop.CommandTrigger += Stop;
        MultiplayerApi.DisconnectedFromGame += Stop;
        CommandStart.CommandTrigger += Start;
        CommandRestart.CommandTrigger += Restart;
        RacingApi.RoundStarted += OnRoundStarted;
        AthStateMachine.Timer.Tick += OnTimerTick;
        CommandSkipBroken.CommandTrigger += SkipBrokenLevel;
        SubStateMachine.StateMachineFinished += Stop;
        Messenger.Notify().Log("started");
    }

    public void Execute()
    {
    }

    public void Exit()
    {
        Messenger.Notify().Log("stopped");
        CommandStop.CommandTrigger -= Stop;
        MultiplayerApi.DisconnectedFromGame -= Stop;
        CommandStart.CommandTrigger -= Start;
        CommandRestart.CommandTrigger -= Restart;
        RacingApi.RoundStarted -= OnRoundStarted;
        AthStateMachine.Timer.Tick -= OnTimerTick;
        CommandSkipBroken.CommandTrigger -= SkipBrokenLevel;
        SubStateMachine.StateMachineFinished -= Stop;
    }

    public void SkipBrokenLevel()
    {
        if (AthStateMachine.Ctx.CurrentLevel != null)
        {
            AthStateMachine.Ctx.CurrentLevel.LevelBroken = true;
            ChatApi.SendMessage("/fs");
        }
    }

    private void Restart()
    {
        StateMachine.TransitionTo(new StateMasterOn(StateMachine));
    }

    private void Stop()
    {
        StateMachine.TransitionTo(new StateMasterOff(StateMachine));
    }


    private void OnTimerTick()
    {
        if (AthStateMachine.Ctx.IsTimeOver())
        {
            Stop();
        }
    }


    private void OnRoundStarted()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
    }

    private void Start()
    {
        Messenger.Notify().LogWarning("already started");
    }
}