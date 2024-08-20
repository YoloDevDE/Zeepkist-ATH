using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Level;
using ZeepSDK.PhotoMode;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthRunning : IState
{
    // Private Fields
    private LevelScriptableObject _currentLevel;

    // Constructor
    public StateAthRunning(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
        AthStateMachine.Timer.Tick += TimerOnTick;
        RacingApi.QuickReset += OnReset;
        RacingApi.PlayerSpawned += OnReset;
        PhotoModeApi.PhotoModeExited += OnPhotomodeExited;
        RacingApi.CrossedFinishLine += OnPlayerResultsChanged;
        RacingApi.RoundEnded += OnRoundEnded;
    }

    private void OnRoundEnded()
    {
        ChatApi.SendMessage("HOW DARE YOU SKIPPING YOU MADMAN >:(");
        StateMachine.TransitionTo(new StateAthSkip(StateMachine));
    }


    public void Execute()
    {
        _currentLevel = LevelApi.CurrentLevel;
        SetServerMessage();
    }

    public void Exit()
    {
        AthStateMachine.Timer.Tick -= TimerOnTick;
        RacingApi.QuickReset -= OnReset;
        RacingApi.PlayerSpawned -= OnReset;
        PhotoModeApi.PhotoModeExited -= OnPhotomodeExited;
        RacingApi.CrossedFinishLine -= OnPlayerResultsChanged;
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    private void OnPlayerResultsChanged(float time)
    {
        ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;

        // Check if the local player beat the author time
        if (networkPlayer.IsLocal && networkPlayer.CurrentResult.Time <= _currentLevel.TimeAuthor)
        {
            StateMachine.TransitionTo(new StateAthPostRunning(StateMachine));
        }
        else
        {
            ChatApi.SendMessage("Looooser.. slooooow...looooooooooooser");
        }
    }

    private void OnPhotomodeExited()
    {
        Pause();
    }


    private void OnReset()
    {
        Pause();
    }

    private void Pause()
    {
        StateMachine.TransitionTo(new StateAthSpawning(StateMachine));
    }


    // Private Methods


    private void TimerOnTick()
    {
        SetServerMessage();
    }

    private void SetServerMessage()
    {
        ChatApi.SendMessage(
            $"/servermessage green 0 ATH running | {TimeFormatter.FormatDuration((int)AthStateMachine.Ctx.CurrentDuration.TotalSeconds)}");
    }
}