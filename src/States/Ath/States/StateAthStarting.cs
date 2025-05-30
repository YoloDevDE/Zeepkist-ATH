using System;
using System.Threading.Tasks;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStarting : AthState
{
    // Constructor
    public StateAthStarting(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    private PlaylistService PlaylistService => PlaylistService.Instance;
    private AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public override IStateMachine
        StateMachine { get; }

    // Public Methods
    public override void Enter()
    {
        RacingApi.RoundEnded += OnRoundEnded;
    }


    public override async void Execute()
    {
        try
        {
            // Sende Startmeldung
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageStarting());

            // Starte neue Playlist mit Fehlerbehandlung
            bool playlistStarted = false;
            int retryCount = 3; // Maximal 3 Versuche

            while (!playlistStarted && retryCount > 0)
            {
                try
                {
                    await PlaylistService.StartNewPlaylist();
                    playlistStarted = true;
                }
                catch (Exception ex)
                {
                    retryCount--;
                    Logger.LogError($"Failed to start playlist: {ex.Message}");

                    if (retryCount <= 0)
                    {
                        Messenger.Notify().LogError("Failed to start playlist after multiple attempts");
                        // Weiter zum nächsten Schritt trotz Fehler
                    }

                    // Kurze Pause vor dem nächsten Versuch
                    await Task.Delay(500);
                }
            }

            // Versuche zum nächsten Level zu springen, auch wenn die Playlist nicht gestartet wurde
            try
            {
                PlaylistService.SkipLevel();
            }
            catch (Exception ex)
            {
                Logger.LogError($"Failed to skip level: {ex.Message}");
                Messenger.Notify().LogError("Error while skipping to the first level");
            }
        }
        catch (Exception ex)
        {
            Logger.LogError($"Execute failed: {ex.Message}\nStack trace: {ex.StackTrace}");
            Messenger.Notify().LogError("Something went wrong while starting the hunt");

            // Optional: Transition zu einem Fehler-State oder Reset-State
            // StateMachine.TransitionTo(new StateAthError(StateMachine));
        }
    }

    public override void Exit()
    {
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    public override void OnAthTimerTick()
    {
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }
}