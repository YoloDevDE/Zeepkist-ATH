using AuthorTimeHunting.Service;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.PlayerSpawned += OnPlayerSpawned;
        RacingApi.RoundEnded += OnRoundEnded;
    }

    public override void Execute()
    {
        AthStateMachine.Ctx.CurrentLevel.Stop();

        Messenger.Notify().LogCustomColors("Author Medal acquired!<br>[Respawn to continue]", Color.white, new Color(0.5f, 0f, 0.5f), 10f);
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageOnARun());
        AthStateMachine.SetServerMessage(true);
    }

    public override void Exit()
    {
        RacingApi.PlayerSpawned -= OnPlayerSpawned;
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    public override void OnAthTimerTick()
    {
        AthStateMachine.SetServerMessage(true);
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }


    // Private Methods
    private void OnPlayerSpawned()
    {
        PlaylistService.SkipLevel();
    }
}