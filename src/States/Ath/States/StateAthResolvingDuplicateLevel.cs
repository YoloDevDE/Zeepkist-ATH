using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthResolvingDuplicateLevel(IStateMachine stateMachine) : AthState
{
    private const int MaxConsecutiveDuplicates = 3;

    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }

    public override async void Execute()
    {
        AthStateMachine.Ctx.ConsecutiveDuplicateCount++;

        if (AthStateMachine.Ctx.ConsecutiveDuplicateCount >= MaxConsecutiveDuplicates)
        {
            int retries = AthStateMachine.Ctx.ConsecutiveDuplicateCount;
            Logger.LogWarning($"StateAthResolvingDuplicateLevel: Duplicate limit reached after {retries} retries. Ending run.");
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageDuplicateLimitReached(retries));
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        // Get current level UID to check if the next candidate would just be the same level again
        string currentUid = AthStateMachine.Ctx.CurrentLevel?.LevelUid;

        if (!PlaylistService.HasValidNextLevel(currentUid))
        {
            Logger.LogWarning("StateAthResolvingDuplicateLevel: No valid next level available (playlist exhausted or only the same level remains). Ending run.");
            Messenger.Notify().LogCustomColors("RandomLevelService warning:<br>Could not fetch a new unique level.<br>Run will stop after this map.", Color.white, Color.red, 8f);
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        await PlaylistService.QueueNextRandomLevel();
        PlaylistService.SkipToLastLevel();
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    private void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }


    public override void OnAthTimerTick() { }
}