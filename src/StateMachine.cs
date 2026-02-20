using System;
using System.Collections.Generic;
using System.Linq;
using System.Linq.Expressions;
using System.Reflection;
using AuthorTimeHunting.Interfaces;
using JetBrains.Annotations;
using Logger = AuthorTimeHunting.Util.Logger;

namespace AuthorTimeHunting;

public class StateMachine
{
    public enum InternalState
    {
        NotInitialized,
        Running,
        Finished,
        Aborted
    }

    private readonly Dictionary<Type, List<IEventTransition>> _eventTransitions = new Dictionary<Type, List<IEventTransition>>();
    private readonly Dictionary<Type, List<Transition>> _transitions = new Dictionary<Type, List<Transition>>();

    private IState _currentState;
    private IState _initialState;

    public string Name = "StateMachine";

    public InternalState State { get; private set; } = InternalState.NotInitialized;

    public StateMachine SetInitial<TState>() where TState : IState, new()
    {
        _initialState = new TState();

        // If you set an initial state, we're now "ready but not running".
        if (State == InternalState.NotInitialized)
        {
            State = InternalState.Finished;
        }

        return this;
    }

    public void Update()
    {
        if (State != InternalState.Running)
        {
            return;
        }

        _currentState?.Update();
        CheckTransitions();
    }

    public void Start()
    {
        if (State == InternalState.Aborted)
        {
            throw new InvalidOperationException($"[{Name}] Cannot Start() because the StateMachine is Aborted. Call Reset() first.");
        }

        Logger.LogInfo($"[{Name}] Starting");
        if (_initialState == null)
        {
            throw new InvalidOperationException($"[{Name}] No initial state set. Call SetInitial<TState>() first.");
        }

        TransitionTo(_initialState.GetType());
        State = InternalState.Running;
    }

    public void Stop()
    {
        Logger.LogInfo($"[{Name}] Stopping");

        TransitionTo(null);
        State = InternalState.Finished;
    }

    public void Abort(string reason = null, Exception exception = null)
    {
        string detail =
            exception != null
                ? exception.Message
                : !string.IsNullOrWhiteSpace(reason)
                    ? reason
                    : "No reason provided";

        Logger.LogError($"[{Name}] Aborting: {detail}");

        TransitionTo(null);
        State = InternalState.Aborted;
    }

    public void Reset()
    {
        Logger.LogInfo($"[{Name}] Resetting");

        // Ensure we fully end any current state + unhook events.
        TransitionTo(null);

        // Clear terminal state.
        State = _initialState != null ? InternalState.Finished : InternalState.NotInitialized;
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
    // Legacy/comfort overload for Action events (parameterlos)
    internal void AddEventTransition(
        Type from,
        Type to,
        Action<Action> subscribe,
        Action<Action> unsubscribe,
        [CanBeNull] Func<bool> guard = null)
    {
        AddEventTransition<Action>(from, to, subscribe, unsubscribe, guard);
    }

    // Generic overload for ANY delegate type (z.B. CrossedFinishLineDelegate(float))
    internal void AddEventTransition<TDelegate>(
        Type from,
        Type to,
        Action<TDelegate> subscribe,
        Action<TDelegate> unsubscribe,
        [CanBeNull] Func<bool> guard = null)
        where TDelegate : Delegate
    {
        if (!_eventTransitions.ContainsKey(from))
        {
            _eventTransitions[from] = new List<IEventTransition>();
        }

        _eventTransitions[from].Add(new EventTransition<TDelegate>
        {
            TargetType = to,
            Subscribe = subscribe,
            Unsubscribe = unsubscribe,
            Guard = guard
        });
    }

    private void HookEventsFor(Type stateType)
    {
        if (!_eventTransitions.TryGetValue(stateType, out List<IEventTransition> list))
        {
            return;
        }

        foreach (IEventTransition t in list)
        {
            t.Hook(this, stateType);
        }
    }

    private void UnhookEventsFor([CanBeNull] Type stateType)
    {
        if (stateType == null)
        {
            return;
        }

        if (!_eventTransitions.TryGetValue(stateType, out List<IEventTransition> list))
        {
            return;
        }

        foreach (IEventTransition t in list)
        {
            t.Unhook();
        }
    }

    // ---------------- STATE SWITCH ----------------

    private void TransitionTo([CanBeNull] Type newStateType)
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
            _currentState = null;
            Logger.LogInfo($"[{Name}] StateMachine is ending - Transition complete");
            return;
        }

        _currentState = (IState)Activator.CreateInstance(newStateType);
        Logger.LogInfo($"[{Name}] Entering {newStateType.Name}");
        _currentState.Enter();
        HookEventsFor(newStateType);
        Logger.LogInfo($"[{Name}] Transition to {newStateType.Name} complete");
    }

    // Helper: called by event transition implementations
    private void TriggerEventTransition(Type fromType, Type targetType, Func<bool> guard)
    {
        // stale / stopped?
        if (_currentState == null || State != InternalState.Running)
        {
            return;
        }

        // stale callback (state changed already)
        if (_currentState.GetType() != fromType)
        {
            return;
        }

        if (guard != null && !guard())
        {
            return;
        }

        TransitionTo(targetType);
    }

    // Build a delegate of ANY signature that ignores parameters and calls callback()
    private static TDelegate BuildDelegate<TDelegate>(Action callback) where TDelegate : Delegate
    {
        MethodInfo invoke = typeof(TDelegate).GetMethod("Invoke");
        if (invoke == null)
        {
            throw new InvalidOperationException($"Delegate type {typeof(TDelegate).Name} has no Invoke method?");
        }

        ParameterExpression[] parameters = invoke.GetParameters()
                                                 .Select(p => Expression.Parameter(p.ParameterType, p.Name))
                                                 .ToArray();

        // call: callback()
        InvocationExpression body = Expression.Invoke(Expression.Constant(callback));

        Expression<TDelegate> lambda = Expression.Lambda<TDelegate>(body, parameters);
        return lambda.Compile();
    }

    // ---------------- INTERNAL TYPES ----------------

    private record Transition(Type TargetType, Func<bool> Condition)
    {
        public Type TargetType { get; } = TargetType;
        public Func<bool> Condition { get; } = Condition;
    }

    private interface IEventTransition
    {
        void Hook(StateMachine sm, Type fromType);
        void Unhook();
    }

    private sealed class EventTransition<TDelegate> : IEventTransition where TDelegate : Delegate
    {
        private Type _fromType;

        private TDelegate _handler; // stored so we can unsubscribe safely
        private StateMachine _sm;
        [CanBeNull] public Func<bool> Guard;
        public Action<TDelegate> Subscribe;
        public Type TargetType;
        public Action<TDelegate> Unsubscribe;

        public void Hook(StateMachine sm, Type fromType)
        {
            _sm = sm;
            _fromType = fromType;

            // this Action is the "core trigger"
            Action trigger = () => _sm.TriggerEventTransition(_fromType, TargetType, Guard);

            _handler = BuildDelegate<TDelegate>(trigger);
            Subscribe(_handler);
        }

        public void Unhook()
        {
            if (_handler == null)
            {
                return;
            }

            Unsubscribe(_handler);
            _handler = null;
        }
    }
}