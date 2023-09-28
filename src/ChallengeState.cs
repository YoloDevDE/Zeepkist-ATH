namespace AuthorTimeHunting;

public abstract class ChallengeState
{
    protected ChallengeState(Challenge challenge)
    {
        Challenge = challenge;
    }

    protected Challenge Challenge { get; }

    public abstract void Enter();
    public abstract void Exit();
    
    
    
}