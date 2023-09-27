namespace AuthorTimeHunting;

public abstract class ChallengeState
{
    protected ChallengeState(Challenge challenge)
    {
        Challenge = challenge;
    }

    public Challenge Challenge { get; }

    public abstract void Enter();
    public abstract void Exit();
}