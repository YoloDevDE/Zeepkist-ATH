using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPenaltySkip : IState
{
    public StateAthPenaltySkip(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    public IStateMachine StateMachine { get; }

    public void Enter()
    {
    }

    public void Execute()
    {
        ChatApi.SendMessage("PunishSkip");
        AthStateMachine.Ctx.Punishments++;
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }

    public void Exit()
    {
    }
}