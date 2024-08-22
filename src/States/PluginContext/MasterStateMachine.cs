using System;
using AuthorTimeHunting.Interfaces;

namespace AuthorTimeHunting.States.PluginContext;

public class MasterStateMachine : IStateMachine
{
    public MasterStateMachine()
    {
        InitialState = new StateOff(this);
        LastState = new StateOff(this);
    }

    public IState LastState { get; set; }
    public event Action StateChanged;

    public IState CurrentState { get; set; }
    public IState InitialState { get; set; }
}