using System;
using AuthorTimeHunting.Interfaces;
using JetBrains.Annotations;

namespace AuthorTimeHunting;

public static class StateMachineExtensions
{
    public static TransitionBuilder<TFrom> From<TFrom>(this StateMachine machine)
        where TFrom : IState, new() => new TransitionBuilder<TFrom>(machine);

    // ================= TRANSITION BUILDER =================

    public class TransitionBuilder<TFrom> where TFrom : IState, new()
    {
        private readonly StateMachine _machine;
        private Func<bool> _condition;

        public TransitionBuilder(StateMachine machine)
        {
            _machine = machine;
        }

        // -------- POLLED --------
        public TransitionBuilder<TFrom> When(Func<bool> condition)
        {
            _condition = condition;
            return this;
        }

        // -------- EVENT (Action) --------
        public EventBuilder<TFrom, Action> On(
            Action<Action> subscribe,
            Action<Action> unsubscribe) => new EventBuilder<TFrom, Action>(_machine, subscribe, unsubscribe);

        // -------- EVENT (ANY delegate) --------
        public EventBuilder<TFrom, TDelegate> On<TDelegate>(
            Action<TDelegate> subscribe,
            Action<TDelegate> unsubscribe)
            where TDelegate : Delegate => new EventBuilder<TFrom, TDelegate>(_machine, subscribe, unsubscribe);

        public TransitionChain<TFrom> To<TTo>()
            where TTo : IState, new()
        {
            if (_condition == null)
            {
                throw new InvalidOperationException("When(...) must be called before To(...).");
            }

            _machine.AddTransition(typeof(TFrom), typeof(TTo), _condition);
            _condition = null;

            return new TransitionChain<TFrom>(_machine);
        }
    }

    // ================= EVENT BUILDER =================

    public sealed class EventBuilder<TFrom, TDelegate>
        where TFrom : IState, new()
        where TDelegate : Delegate
    {
        private readonly StateMachine _machine;
        private readonly Action<TDelegate> _subscribe;
        private readonly Action<TDelegate> _unsubscribe;
        [CanBeNull] private Func<bool> _guard;

        public EventBuilder(
            StateMachine machine,
            Action<TDelegate> subscribe,
            Action<TDelegate> unsubscribe)
        {
            _machine = machine;
            _subscribe = subscribe;
            _unsubscribe = unsubscribe;
        }

        // optional guard
        public EventBuilder<TFrom, TDelegate> When(Func<bool> guard)
        {
            _guard = guard;
            return this;
        }

        public TransitionChain<TFrom> To<TTo>()
            where TTo : IState, new()
        {
            _machine.AddEventTransition(typeof(TFrom), typeof(TTo), _subscribe, _unsubscribe, _guard);
            return new TransitionChain<TFrom>(_machine);
        }
    }

    // ================= OR-CHAIN =================

    public sealed class TransitionChain<TFrom> where TFrom : IState, new()
    {
        private readonly StateMachine _machine;

        public TransitionChain(StateMachine machine)
        {
            _machine = machine;
        }

        public TransitionBuilder<TFrom> Or() => new TransitionBuilder<TFrom>(_machine);

        public static implicit operator StateMachine(TransitionChain<TFrom> chain) => chain._machine;
    }
}