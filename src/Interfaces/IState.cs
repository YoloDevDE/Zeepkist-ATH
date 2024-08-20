namespace AuthorTimeHunting.Interfaces;

public interface IState
{
    public IStateMachine StateMachine { get; }

    public IStateMachine SubStateMachine
    {
        get => null;
        set => SubStateMachine = value;
    }

    void Enter();
    void Execute();
    void Exit();
}