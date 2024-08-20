namespace AuthorTimeHunting.Interfaces;

public interface IStateMachine
{
    delegate void StateMachineFinishedDelegate();

    IState CurrentState { get; set; }
    IState InitialState { get; }
    IState LastState { get; }

    event StateMachineFinishedDelegate OnStateMachineFinished;

    void TransitionTo(IState nextState)
    {
        CurrentState?.SubStateMachine?.Stop();
        CurrentState?.Exit();
        if (nextState != null)
        {
            CurrentState = nextState;
            CurrentState.Enter();
            CurrentState.Execute();
        }
        else
        {
            Stop();
        }
    }

    void Stop()
    {
        if (CurrentState == null)
        {
            return;
        }

        CurrentState.SubStateMachine?.Stop();
        CurrentState.StateMachine.TransitionTo(CurrentState.StateMachine.LastState);
        CurrentState.Exit();
    }
}