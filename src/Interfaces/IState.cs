namespace AuthorTimeHunting.Interfaces;

public interface IState
{
    public IStateMachine StateMachine { get; }

    public IStateMachine SubStateMachine => null;
    void Enter();
    void Execute();
    void Exit();
}