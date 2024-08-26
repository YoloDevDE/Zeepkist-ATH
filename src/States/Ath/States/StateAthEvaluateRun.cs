using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthEvaluateRun : IState
{
    public StateAthEvaluateRun(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
        PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
        Level currentLevel = AthStateMachine.Ctx.CurrentLevel;

        if (currentResult == null)
        {
            StateMachine.TransitionTo(new StateAthNothingUnlocked(StateMachine));
            return;
        }

        if (currentResult.Time <= currentLevel.AuthorTime)
        {
            StateMachine.TransitionTo(new StateAthAuthorMedalUnlocked(StateMachine));
        }
        else if (currentResult.Time <= currentLevel.GoldTime && !currentLevel.GoldSkipUnlocked)
        {
            StateMachine.TransitionTo(new StateAthGoldMedalUnlocked(StateMachine));
        }
        else
        {
            StateMachine.TransitionTo(new StateAthNothingUnlocked(StateMachine));
        }
    }

    public void Exit()
    {
    }
}