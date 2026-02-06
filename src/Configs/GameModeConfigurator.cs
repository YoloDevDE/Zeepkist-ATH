using AuthorTimeHunting.Enums;
using AuthorTimeHunting.States.SoloAth;
using ZeepkistClient;

namespace AuthorTimeHunting.Configs;

public abstract class GameModeConfigurator
{
    public static StateMachine SoloAthMode()
    {
        StateMachine soloAthMode = new StateMachine();
        soloAthMode
            .SetInitial<SoloAthInit>();
        soloAthMode.Name = "SoloAthMode";
        soloAthMode
            .From<SoloAthInit>()
            .When(() => ZeepkistNetwork.CurrentLobby.GameState == (int)ZeepkistLobbyState.RACING)
            .To<SoloAthStarting>()
            .Or()
            .When(() => true)
            .To<SoloAthWaitingForLevelToLoadWhileStarting>()
            ;

        soloAthMode
            .From<SoloAthStarting>()
            .When(() => ZeepkistNetwork.CurrentLobby.GameState == (int)ZeepkistLobbyState.ENDING)
            .To<SoloAthRoundEnding>();

        soloAthMode
            .From<SoloAthRoundEnding>()
            .When(() => ZeepkistNetwork.CurrentLobby.GameState == (int)ZeepkistLobbyState.PODIUM)
            .To<SoloAthPodium>();

        soloAthMode
            .From<SoloAthPodium>()
            .When(() => PlayerManager.Instance.currentMaster.setupScript.LevelLoadingUI.IsOpen)
            .To<SoloAthLevelLoading>();

        soloAthMode
            .From<SoloAthLevelLoading>()
            .When(() => ZeepkistNetwork.CurrentLobby.GameState == (int)ZeepkistLobbyState.RACING)
            .To<SoloAthStarting>();

        return soloAthMode;
    }
}