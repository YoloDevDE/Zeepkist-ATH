using System;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Master.States;

namespace AuthorTimeHunting.States.Master.StateMachine;

public class MasterStateMachine : IStateMachine
{
    public MasterStateMachine()
    {
        InitialState = new StateMasterOff(this);
        FinalState = new StateMasterOff(this);
    }

    public event Action StateMachineFinished;

    public IState FinalState { get; set; }
    public IState CurrentState { get; set; }
    public IState InitialState { get; set; }

    public void InvokeFinish()
    {
        StateMachineFinished?.Invoke();
    }
}