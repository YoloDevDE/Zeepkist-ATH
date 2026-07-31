using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistNetworking;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingDuplicateLevel(IStateMachine stateMachine) : AthState
{
    private const int MaxConsecutiveDuplicates = 3;

    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter() { }

    public override async void Execute()
    {
        AthStateMachine.Ctx.ConsecutiveDuplicateCount++;

        if (AthStateMachine.Ctx.ConsecutiveDuplicateCount >= MaxConsecutiveDuplicates)
        {
            int retries = AthStateMachine.Ctx.ConsecutiveDuplicateCount;
            Logger.LogWarning($"StateAthResolvingDuplicateLevel: Duplicate limit reached after {retries} retries. Ending run.");
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.Messages.DuplicateLimitReached(retries));
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        try
        {
            OnlineZeeplevel newLevel = await RandomLevelService.Instance.DrawRandomLevelAsync();
            PlaylistService.AddLevelToCurrentPlaylist(newLevel);
            PlaylistService.SkipToNextLevel();
        }
        catch (Exception ex)
        {
            // async void - nothing above us can catch this.
            Logger.LogError($"StateAthResolvingDuplicateLevel: Could not draw a replacement level: {ex.Message}");
            Messenger.Notify().LogError("Could not find another level to play");
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
        }
    }

    public override void Exit() { }

    public override void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }


    public override void OnAthTimerTick() { }
}