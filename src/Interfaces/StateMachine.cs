using System;
using System.Collections.Generic;
using System.Linq;

namespace AuthorTimeHunting.Interfaces;

public class StateMachine
{
    private readonly Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();
    private IState _currentState;

    public StateMachine SetInitial<TState>() where TState : IState, new()
    {
        _currentState = new TState();
        _currentState.Enter();
        return this;
    }

    public void Update()
    {
        _currentState?.Update();
        CheckTransitions();
    }

    private void CheckTransitions()
    {
        Type currentType = _currentState.GetType();
        if (!_transitions.TryGetValue(currentType, out List<Transition> transitions))
        {
            return;
        }

        foreach (Transition t in transitions.Where(t => t.Condition()))
        {
            SwitchTo(t.TargetType);
            break;
        }
    }

    private void SwitchTo(Type newStateType)
    {
        _currentState.Exit();
        _currentState = (IState)Activator.CreateInstance(newStateType);
        _currentState.Enter();
    }

    internal void AddTransition(Type from, Type to, Func<bool> condition)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new List<Transition>();
        }

        _transitions[from].Add(new Transition(to, condition));
    }

    private record Transition(Type TargetType, Func<bool> Condition)
    {
        public Type TargetType { get; } = TargetType;
        public Func<bool> Condition { get; } = Condition;
    }
}