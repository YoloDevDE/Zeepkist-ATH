using AuthorTimeHunting.Interfaces;
using AuthorTimeHunting.States.Ath.StateMachine;
using ZeepkistClient;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthLoadingLevel(IStateMachine stateMachine) : AthState
{
    public override IStateMachine StateMachine { get; } = stateMachine;


    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
    }


    public override void Execute()
    {
        string levelInfo = AthStateMachine.Ctx.CurrentLevel != null ? $"Level: <b>{AthStateMachine.Ctx.CurrentLevel.StatusString}</b>" : "Good Luck Have Fun!";
        PlayerManager.Instance.currentMaster.OnlineGameplayUI.RoundOverText.SetText($"<#ff01d2ff><b>A</b>uthor <b>T</b>ime <b>H</b>unting</color> <sprite=\"Zeepkist\" name=\"Smile\"><br>{levelInfo}");
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
    }

    public override void OnAthTimerTick() { }

    private void OnLevelLoaded()
    {
        if (!AthStateMachine.Ctx.IsTimeOver() && (Plugin.Instance.MyConfig.RandomPlaylist.Value || AthStateMachine.Ctx.Levels.Count < ZeepkistNetwork.CurrentLobby.Playlist.Count))
        {
            StateMachine.TransitionTo(new StateAthProcessingLevel(StateMachine));
            return;
        }

        StateMachine.TransitionTo(new StateAthStopping(StateMachine));
    }
}