using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public abstract class AthState : IState
{
    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;
    public PlaylistService PlaylistService => PlaylistService.Instance;
    public abstract IStateMachine StateMachine { get; }
    public abstract void Enter();
    public abstract void Execute();
    public abstract void Exit();

    public abstract void OnAthTimerTick();
    public virtual void OnRoundStarted() { }
    public virtual void OnRoundEnded() { }
    public virtual void OnPlayerSpawned() { }
    public virtual void OnCrossedFinishLine(float time) { }
    public virtual void OnLevelLoaded() { }
    public virtual void OnPhotoModeEntered() { }
}