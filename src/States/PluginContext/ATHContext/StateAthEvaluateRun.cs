using AuthorTimeHunting.Interfaces;
using ZeepkistClient;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

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
        if (networkPlayer.CurrentResult.Time <= AthStateMachine.Ctx.CurrentLevel.AuthorTime)
        {
            StateMachine.TransitionTo(new StateAthAuthorMedalUnlocked(StateMachine));
            return;
        }

        if (networkPlayer.CurrentResult.Time <= AthStateMachine.Ctx.CurrentLevel.GoldTime && !AthStateMachine.Ctx.CurrentLevel.GoldSkipUnlocked)
        {
            StateMachine.TransitionTo(new StateAthGoldMedalUnlocked(StateMachine));
            return;
        }

        StateMachine.TransitionTo(new StateAthNothingUnlocked(StateMachine));
    }

    public void Exit()
    {
    }
}