using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.Chat;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthRunning : IState
{
    // Private Fields

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
        RacingApi.RoundStarted += OnRoundNztStarted;
        RacingApi.CrossedFinishLine += OnCrossedFinishLine;
        RacingApi.RoundEnded += OnRoundNztEnded;
    }


    public void Execute()
    {
        SetServerMessage();
        RunningMessage();
    }

    public void Exit()
    {
        AthStateMachine.Timer.Tick -= TimerOnTick;
        RacingApi.RoundStarted -= OnRoundNztStarted;
        RacingApi.CrossedFinishLine -= OnCrossedFinishLine;
        RacingApi.RoundEnded -= OnRoundNztEnded;
    }

    private void RunningMessage()
    {
        ChatApi.ClearChat();
        Messenger.SendChat(
            new Message.Builder()
                .AddLine($"{AthStateMachine.Ctx.CurrentLevel.Name} by {AthStateMachine.Ctx.CurrentLevel.Author}")
                .AddBreakSpace()
                .AddSeperator("Goals")
                .AddBreakSpace()
                .AddKeyValue("AT", $"{AthStateMachine.Ctx.CurrentLevel.AuthorTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddKeyValue("Gold", $"{AthStateMachine.Ctx.CurrentLevel.GoldTime.GetFormattedTime()}")
                .AddBreakSpace()
                .AddSeperator("Stats")
                .AddBreakSpace()
                .AddKeyValue("AT Medals", $"{AthStateMachine.Ctx.AuthorMedals}")
                .AddBreakSpace()
                .AddKeyValue("Attempts", $"{AthStateMachine.Ctx.CurrentLevel.Attempts}")
                .AddBreakSpace()
                .AddSeperator("Misc")
                .AddBreakSpace()
                .AddKeyValue("Gold Skip", $"{(AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked ? "  unlocked :zaagbladpad:" : "  locked :zaagbladpadrood:")}")                
                .AddBreakSpace()
                .AddKeyValue("Free Skips", $"{AthStateMachine.Ctx.FreeSkips}")
                .Build()
                .ToString()
        );
    }

    private void OnRoundNztEnded()
    {
        StateMachine.TransitionTo(new StateAthSkip(StateMachine));
    }

    private void OnCrossedFinishLine(float time)
    {
        ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;

        // Check if the local player beat the author time
        if (networkPlayer.CurrentResult.Time <= AthStateMachine.Ctx.CurrentLevel.AuthorTime)
        {
            // Purple color for "Authortime achieved"
            Messenger.Notify().LogCustomColors($"You've got the Author Medal", Color.white, new Color(0.5f, 0f, 0.5f), 7.5f);
            StateMachine.TransitionTo(new StateAthPostRunning(StateMachine));
            return;
        }

        if (networkPlayer.CurrentResult.Time <= AthStateMachine.Ctx.CurrentLevel.GoldTime)
        {
            AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked = true;
            // Gold color for "You unlocked the 'Gold Skip'"
            Messenger.Notify().LogCustomColors($"You've got the Gold Medal !<br>Gold Skip: Unlocked", Color.black, new Color(1f, 0.84f, 0f), 7.5f);
        }

        Pause();
    }

    private void OnRoundNztStarted()
    {
        AthStateMachine.Ctx.CurrentLevel.Attempts++;
        RunningMessage();
    }

    private void Pause()
    {
        StateMachine.TransitionTo(new StateAthPausing(StateMachine));
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