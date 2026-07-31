using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForRespawn(IStateMachine stateMachine) : AthState
{
    private bool _hasShownAuthorMedal;

    public override IStateMachine StateMachine { get; } = stateMachine;

    public override void Enter()
    {
        _hasShownAuthorMedal = false;
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

    public override void Exit() { }

    public override void OnAthTimerTick()
    {
        AthStateMachine.SetServerMessage(true);
    }

    public override void OnRoundEnded()
    {
        MedalTextHelper.ClearMedalText();
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }


    // Private Methods
    public override void OnPlayerSpawned()
    {
        MedalTextHelper.ClearMedalText();
        PlaylistService.SkipLevel();
    }
}