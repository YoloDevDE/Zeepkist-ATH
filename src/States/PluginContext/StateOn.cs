using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.PluginContext.ATHContext;
using AuthorTimeHunting.Util;
using ZeepSDK.Chat;
using ZeepSDK.Multiplayer;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext;

public class StateOn : IState
{
    // Constructor
    public StateOn(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
        SubStateMachine = new AthStateMachine();
    }


    public IStateMachine SubStateMachine { get; }

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        CommandStop.CommandTrigger += StopChallenge;
        CommandStart.CommandTrigger += StartChallenge;
        CommandRestart.CommandTrigger += ReStartChallenge;
        MultiplayerApi.DisconnectedFromGame += StopChallenge;
        RacingApi.RoundStarted += OnRoundStarted;
        SubStateMachine.StateChanged += Update;
    }

    public void Execute()
    {
    }


    public void Exit()
    {
        CommandStop.CommandTrigger -= StopChallenge;
        CommandStart.CommandTrigger -= StartChallenge;
        CommandRestart.CommandTrigger -= ReStartChallenge;
        MultiplayerApi.DisconnectedFromGame -= StopChallenge;
        RacingApi.RoundStarted -= OnRoundStarted;
        SubStateMachine.StateChanged -= Update;
    }

    private void Update()
    {
        if (SubStateMachine.CurrentState != SubStateMachine.LastState)
        {
            return;
        }

        StateMachine.Reset();
        ChatApi.SendMessage("Stopped");
    }

    private void OnRoundStarted()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
    }


    // Private Methods
    private void StartChallenge()
    {
        Messenger.Notify().LogWarning("already started");
    }

    private void StopChallenge()
    {
        Messenger.Notify().LogSuccess("stopped");
        StateMachine.TransitionTo(new StateOff(StateMachine));
    }

    private void ReStartChallenge()
    {
        SubStateMachine.TransitionTo(SubStateMachine.LastState);
    }
}