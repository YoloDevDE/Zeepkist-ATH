namespace AuthorTimeHunting.Interfaces;

public interface IState
{
    void Enter();
    void Exit();
    void Update();
}