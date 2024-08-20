using AuthorTimeHunting.Commands;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.PluginContext.ATHContext;
using AuthorTimeHunting.Util;
using ZeepSDK.ChatCommands;
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
    public IState CurrentState { get; set; }

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
        
        SubStateMachine.TransitionTo(new StateAthStarting(SubStateMachine));
    }

    public void Execute()
    {
        // No additional implementation needed in Execute for this state
    }

    public void Exit()
    {
        SubStateMachine.Stop();
        CommandStop.CommandTrigger -= StopChallenge;
        CommandStart.CommandTrigger -= StartChallenge;
        CommandRestart.CommandTrigger -= ReStartChallenge;
        MultiplayerApi.DisconnectedFromGame -= StopChallenge;
        RacingApi.RoundStarted -= OnRoundStarted;
    }

    private void OnRoundStarted()
    {
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.TimeLeftText.enabled = false;
    }

    // Events
    public event IStateMachine.StateMachineFinishedDelegate OnStateMachineFinished;

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
        Messenger.Notify().LogWarning("restarted");
        StateMachine.TransitionTo(new StateOn(StateMachine));
    }
}