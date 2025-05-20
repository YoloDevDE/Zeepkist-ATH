using System.Threading.Tasks;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthStarting : IState
{
    // Constructor
    public StateAthStarting(IStateMachine stateMachine)
    {
        StateMachine = stateMachine;
    }

    public AthStateMachine AthStateMachine => (AthStateMachine)StateMachine;

    // Properties
    public IStateMachine StateMachine { get; }

    // Public Methods
    public void Enter()
    {
    }

    public async void Execute()
    {
        // Wait until the GameState is not 0
        await WaitUntilGameStateNotZero();

        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageStarting());
        AthStateMachine.StartTimer();
        StateMachine.TransitionTo(new StateAthPreparePlaylist(StateMachine));
    }

    public void Exit()
    {
    }

    private async Task WaitUntilGameStateNotZero()
    {
        while (ZeepkistNetwork.CurrentLobby.GameState != 0)
        {
            await Task.Delay(1000); // Check every 100ms to not block the thread
        }

        await Task.Delay(1000); // Check every 100ms to not block the thread
    }
}