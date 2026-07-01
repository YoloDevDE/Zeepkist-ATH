using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepSDK.Racing;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(IStateMachine stateMachine) : AthState
{
    private bool _hasShownAuthorMedal;

    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        _hasShownAuthorMedal = false;
        RacingApi.PlayerSpawned += OnPlayerSpawned;
        RacingApi.RoundEnded += OnRoundEnded;
    }

    public override void Execute()
    {
        AthStateMachine.Ctx.CurrentLevel.Stop();

        if (!_hasShownAuthorMedal)
        {
            Messenger.Notify().LogCustomColors("Author time claimed!<br>[Respawn to continue]", Color.white, new Color(0.5f, 0f, 0.5f), 5f);

            double lastRunTime = AthStateMachine.Ctx.LastRunTime;

            if (lastRunTime < 0)
            {
                lastRunTime = ZeepkistNetwork.LocalPlayer?.CurrentResult?.Time ?? -1;
            }

            if (lastRunTime >= 0)
            {
                MedalTextHelper.SetMedalProgressText(AthStateMachine.Ctx.CurrentLevel, lastRunTime, AthStateMachine.Ctx.LastRunMedalWasNew);
            }
            else
            {
                MedalTextHelper.SetMedalText("<b><#50E451>NEW</color></b> medal: <b><#fd51ff>AUTHOR</color></b><br><#A7A7A7>(respawn to skip)</color>");
            }

            ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageAuthorMedalClaimed());
            _hasShownAuthorMedal = true;
        }

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
        MedalTextHelper.ClearMedalText();
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }


    // Private Methods
    private void OnPlayerSpawned()
    {
        MedalTextHelper.ClearMedalText();
        string currentUid = AthStateMachine.Ctx.CurrentLevel?.LevelUid;

        if (!PlaylistService.HasValidNextLevel(currentUid))
        {
            Logger.LogWarning("StateAthWaitingForRespawn: No valid next level available after AT claim. Ending run.");
            Messenger.Notify().LogCustomColors("RandomLevelService warning:<br>Could not fetch a new unique level.<br>Run will stop after this map.", Color.white, Color.red, 8f);
            StateMachine.TransitionTo(new StateAthStopping(StateMachine));
            return;
        }

        PlaylistService.SkipLevel();
    }
}