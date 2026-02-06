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

        // -------- EVENT --------
        public EventBuilder<TFrom> On(
            Action<Action> subscribe,
            Action<Action> unsubscribe) => new EventBuilder<TFrom>(_machine, subscribe, unsubscribe);

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

    public sealed class EventBuilder<TFrom> where TFrom : IState, new()
    {
        private readonly StateMachine _machine;
        private readonly Action<Action> _subscribe;
        private readonly Action<Action> _unsubscribe;
        [CanBeNull] private Func<bool> _guard;

        public EventBuilder(
            StateMachine machine,
            Action<Action> subscribe,
            Action<Action> unsubscribe)
        {
            _machine = machine;
            _subscribe = subscribe;
            _unsubscribe = unsubscribe;
        }

        public EventBuilder<TFrom> When(Func<bool> guard)
        {
            _guard = guard;
            return this;
        }

        public TransitionChain<TFrom> To<TTo>()
            where TTo : IState, new()
        {
            _machine.AddEventTransition(
                typeof(TFrom),
                typeof(TTo),
                _subscribe,
                _unsubscribe,
                _guard);

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