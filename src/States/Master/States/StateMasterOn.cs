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
        CommandSkipBroken.CommandTrigger += SkipBrokenLevel;
        SubStateMachine.StateMachineFinished += Stop;
        AthStateMachine.StartTimer();
        Messenger.Notify().Log("started");
    }

    public void Execute() { }

    public void Exit()
    {
        Messenger.Notify().Log("stopped");
        AthStateMachine.StopTimer();
        CommandStop.CommandTrigger -= Stop;
        MultiplayerApi.DisconnectedFromGame -= Stop;
        CommandStart.CommandTrigger -= Start;
        CommandRestart.CommandTrigger -= Restart;
        RacingApi.RoundStarted -= OnRoundStarted;
        CommandSkipBroken.CommandTrigger -= SkipBrokenLevel;
        SubStateMachine.StateMachineFinished -= Stop;
    }

    private void OnRoundStarted()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
    }


    private void Restart()
    {
        StateMachine.TransitionTo(new StateMasterOn(StateMachine));
    }

    private void SkipBrokenLevel()
    {
        if (AthStateMachine.Ctx.CurrentLevel != null)
        {
            AthStateMachine.Ctx.CurrentLevel.LevelBroken = true;
            ChatApi.SendMessage("/fs");
        }
    }

    private void Start()
    {
        Messenger.Notify().LogWarning("already started");
    }

    private void Stop()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = true;
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = "Thanks for playing ATH :)";
        StateMachine.TransitionTo(new StateMasterOff(StateMachine));
    }
}