using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
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
            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageDuplicateLimitReached(retries));
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        // Get current level UID to check if the next candidate would just be the same level again
        string currentUid = AthStateMachine.Ctx.CurrentLevel?.LevelUid;


        OnlineZeeplevel newLevel = await RandomLevelService.Instance.DrawRandomLevelAsync();
        PlaylistService.AddLevelToCurrentPlaylist(newLevel);
        PlaylistService.SkipToNextLevel();
    }

    public override void Exit() { }

    public override void OnLevelLoaded()
    {
        StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
    }


    public override void OnAthTimerTick() { }
}