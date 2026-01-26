using System;

namespace AuthorTimeHunting.Interfaces;

public static class StateMachineExtensions
{
    public static TransitionBuilder<TFrom> From<TFrom>(this StateMachine machine)
        where TFrom : IState, new() => new TransitionBuilder<TFrom>(machine);

    public class TransitionBuilder<TFrom> where TFrom : IState, new()
    {
        private readonly StateMachine _machine;
        private Func<bool> _condition;

        public TransitionBuilder(StateMachine machine)
        {
            _machine = machine;
        }

        public TransitionBuilder<TFrom> When(Func<bool> condition)
        {
            _condition = condition;
            return this;
        }

        // alias for "When" – stylistic choice
        public TransitionBuilder<TFrom> On(Func<bool> condition) => When(condition);

        public StateMachine TransitionTo<TTo>()
            where TTo : IState, new()
        {
            _machine.AddTransition(typeof(TFrom), typeof(TTo), _condition);
            return _machine;
        }
    }
}