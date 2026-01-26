using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.States;

namespace AuthorTimeHunting.Configs;

public class GameModeConfigurator
{
    public StateMachine DefaultMode()
    {
        StateMachine defaultMode = new StateMachine();
        defaultMode
            .SetInitial<AthStarting>();
        defaultMode
            .From<AthStarting>()
                .When(() => true)
                .TransitionTo<AthPlaying>();
    }
}

