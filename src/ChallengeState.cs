namespace AuthorTimeHunting;

public abstract class ChallengeState
{
    public abstract void Enter(Challenge challenge);
    public abstract void Transition(Challenge challenge);
    public abstract void OnLevelLoaded(Challenge challenge);
}