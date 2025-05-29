using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStarting : AthState
{
    // Constructor
    public StateAthStarting(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    private PlaylistService PlaylistService => PlaylistService.Instance;
    private AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public override IStateMachine
        StateMachine { get; }

    // Public Methods
    public override void Enter()
    {
        RacingApi.RoundEnded += OnRoundEnded;
    }


    public override async void Execute()
    {
        ChatMessageService.SendCustomMessage(AthStateMachine.Ctx.MessageStarting());
        await PlaylistService.StartNewPlaylist();
        PlaylistService.SkipLevel();
    }

    public override void Exit()
    {
        RacingApi.RoundEnded -= OnRoundEnded;
    }

    public override void OnAthTimerTick()
    {
        AthStateMachine.Ctx.PauseTimeInSeconds += 1;
    }

    private void OnRoundEnded()
    {
        StateMachine.TransitionTo(new StateAthLoadingNewLevel(StateMachine));
    }
}