using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPausing(IStateMachine stateMachine) : AthState
{
    private bool _hasShownMedalRoundOverText;
    // Constructor


    // Properties
    public override IStateMachine StateMachine { get; } = stateMachine;

    // Public Methods
    public override void Enter()
    {
        _hasShownMedalRoundOverText = false;
    }

    public override void Execute()
    {
        if (!_hasShownMedalRoundOverText)
        {
            PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer?.CurrentResult;
            double lastRunTime = AthStateMachine.Ctx.LastRunTime > 0 ? AthStateMachine.Ctx.LastRunTime : currentResult?.Time ?? -1;
            bool hasMedalToShow = AthStateMachine.Ctx.LastRunMedalStatus is Level.LevelStatus.AUTHOR or Level.LevelStatus.GOLD;

            if (currentResult != null && hasMedalToShow && lastRunTime >= 0)
            {
                MedalTextHelper.SetMedalProgressText(AthStateMachine.Ctx.CurrentLevel, lastRunTime, AthStateMachine.Ctx.LastRunMedalWasNew);
                _hasShownMedalRoundOverText = true;
            }
        }

        AthStateMachine.Ctx.ResetRetries();
        AthStateMachine.SetServerMessage(true);
    }

    public override void Exit() { }

    public override void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthEvaluateSkip(StateMachine));
    }

    // Private Methods
    public override void OnAthTimerTick()
    {
        AthStateMachine.SetServerMessage(true);
    }


    public override void OnRoundStarted()
    {
        StateMachine.TransitionTo(new StateAthOnARun(StateMachine));
    }

    public override void OnPhotoModeEntered()
    {
        OnRoundStarted();
    }

    public override void OnPlayerSpawned()
    {
        MedalTextHelper.ClearMedalText();
    }
}