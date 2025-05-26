using System.Threading.Tasks;
using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepSDK.Chat;

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
        MessageSenderService.SendLocalMessage(AthStateMachine.Ctx.MessageStarting());
        await WaitUntilGameStateNotZero();
        await PlaylistService.Instance.StartNewPlaylist();
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.text = "<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>GL HF!</b>";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.enableWordWrapping = true;
        await WaitUntilGameStateNotZero();
        ChatApi.SendMessage("/fs");
        AthStateMachine.StartTimer();
        StateMachine.TransitionTo(new StateAthLevelSummary(StateMachine));
    }

    public void Exit()
    {
    }

    private async Task WaitUntilGameStateNotZero()
    {
        while (ZeepkistNetwork.CurrentLobby.GameState != 0)
        {
            await Task.Delay(100);
        }

        await Task.Delay(5000);
    }
}