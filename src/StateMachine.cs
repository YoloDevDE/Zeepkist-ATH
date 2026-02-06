using System;
using System.Collections.Generic;
using System.Linq;
using AuthorTimeHunting.Interfaces;
using JetBrains.Annotations;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting;

public class StateMachine
{
    private readonly Dictionary<Type, List<EventTransition>> _eventTransitions = new Dictionary<Type, List<EventTransition>>();
    private readonly Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();

    private IState _currentState;
    private IState _initialState;
    public string Name = "StateMachine";

    public StateMachine SetInitial<TState>() where TState : IState, new()
    {
        _initialState = new TState();
        return this;
    }

    public void Update()
    {
        _currentState?.Update();
        CheckTransitions();
    }

    public void Start()
    {
        Logger.LogInfo($"[{Name}] Starting");
        TransitionTo(_initialState.GetType());
    }

    public void Stop()
    {
        Logger.LogInfo($"[{Name}] Stopping");
        TransitionTo(null);
    }

    // ---------------- POLLED TRANSITIONS ----------------

    private void CheckTransitions()
    {
        if (_currentState == null)
        {
            return;
        }

        Type currentType = _currentState.GetType();


        if (!_transitions.TryGetValue(currentType, out List<Transition> transitions))
        {
            return;
        }

        foreach (Transition t in transitions.Where(t => t.Condition()))
        {
            TransitionTo(t.TargetType);
            break;
        }
    }

    internal void AddTransition(Type from, Type to, Func<bool> condition)
    {
        if (!_transitions.ContainsKey(from))
        {
            _transitions[from] = new List<Transition>();
        }

        _transitions[from].Add(new Transition(to, condition));
    }

    // ---------------- EVENT TRANSITIONS ----------------

    internal void AddEventTransition(
        Type from,
        Type to,
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        [CanBeNull] Func<bool> guard = null)
    {
        if (!_eventTransitions.ContainsKey(from))
        {
            _eventTransitions[from] = new List<EventTransition>();
        }

        _eventTransitions[from].Add(new EventTransition
        {
            TargetType = to,
            Subscribe = subscribe,
            Unsubscribe = unsubscribe,
            Guard = guard
        });
    }

    private void HookEventsFor(Type stateType)
    {
        if (!_eventTransitions.TryGetValue(stateType, out List<EventTransition> list))
        {
            return;
        }

        foreach (EventTransition t in list)
        {
            Type fromType = stateType;

            t.Handler = () =>
            {
                // Safety: ignore stale callbacks
                if (_currentState.GetType() != fromType)
                {
                    return;
                }

                if (t.Guard != null && !t.Guard())
                {
                    return;
                }

                TransitionTo(t.TargetType);
            };

            t.Subscribe(t.Handler);
        }
    }

    private void UnhookEventsFor([CanBeNull] Type stateType)
    {
        if (stateType == null)
        {
            return;
        }

        if (!_eventTransitions.TryGetValue(stateType, out List<EventTransition> list))
        {
            return;
        }

        foreach (EventTransition t in list)
        {
            if (t.Handler == null)
            {
                continue;
            }

            t.Unsubscribe(t.Handler);
            t.Handler = null;
        }
    }

    // ---------------- STATE SWITCH ----------------

    private void TransitionTo(Type newStateType)
    {
        if (_currentState != null)
        {
            if (newStateType != null)
            {
                Logger.LogInfo($"[{Name}] Transitioning from {_currentState.GetType().Name} to {newStateType.Name}");
            }

            Logger.LogInfo($"[{Name}] Exiting {_currentState.GetType().Name}");
            UnhookEventsFor(_currentState.GetType());
            _currentState.Exit();
        }

        if (newStateType == null)
        {
            Logger.LogInfo($"[{Name}] StateMachine is ending - Transition from {_currentState?.GetType().Name} complete");
            return;
        }

        _currentState = (IState)Activator.CreateInstance(newStateType);
        Logger.LogInfo($"[{Name}] Entering {newStateType.Name}");
        _currentState.Enter();
        HookEventsFor(newStateType);
        Logger.LogInfo($"[{Name}] Transition to {newStateType.Name} complete");
    }

    // ---------------- INTERNAL TYPES ----------------

    private record Transition(Type TargetType, Func<bool> Condition)
    {
        public Type TargetType { get; } = TargetType;
        public Func<bool> Condition { get; } = Condition;
    }

    private sealed class EventTransition
    {
        [CanBeNull] public Func<bool> Guard;
        [CanBeNull] public Action Handler;
        public Action<Action> Subscribe;
        public Type TargetType;
        public Action<Action> Unsubscribe;
    }
}