using AuthorTimeHunting.Interfaces;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public abstract class AthState : IState
{
    public abstract IStateMachine StateMachine { get; }
    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();

    public abstract void OnAthTimerTick();
}