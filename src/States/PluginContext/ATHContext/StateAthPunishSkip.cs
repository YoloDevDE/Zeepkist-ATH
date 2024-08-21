using AuthorTimeHunting.Interfaces;
using ZeepSDK.Chat;

namespace AuthorTimeHunting.States.PluginContext.ATHContext;

public class StateAthPunishSkip : IState
{
    public StateAthPunishSkip(IStateMachine stateMachine)
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
        StateMachine.TransitionTo(new StateAthLoading(StateMachine));
    }

    public void Exit()
    {
    }
}