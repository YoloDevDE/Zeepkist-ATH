using AuthorTimeHunting.Enums;
using AuthorTimeHunting.Guards;
using AuthorTimeHunting.States.SoloAth;
using ZeepSDK.Racing;

namespace AuthorTimeHunting.Configs;

public abstract class GameModeConfigurator
{
    public static StateMachine SoloAthMode()
    {
        StateMachine soloAthMode = new StateMachine();
        soloAthMode
            .SetInitial<SoloAthInit>();
        soloAthMode.Name = "SoloAthMode";

        // ================= BOOT / START =================

        soloAthMode
            .From<SoloAthInit>()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.RACING))
            .To<SoloAthStarting>()
            .Or()
            .When(() => true)
            .To<SoloAthWaitingForLevelToLoadWhileStarting>();

        soloAthMode
            .From<SoloAthWaitingForLevelToLoadWhileStarting>()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.RACING))
            .To<SoloAthStarting>();

        soloAthMode
            .From<SoloAthStarting>()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.ENDING))
            .To<SoloAthWaitingForFirstLevel>();

        // ================= LEVEL LOOP =================

        soloAthMode
            .From<SoloAthWaitingForFirstLevel>()
            .On<RoundStartedDelegate>(sub => RacingApi.RoundStarted += sub, unsub => RacingApi.RoundStarted -= unsub)
            .To<SoloAthAttemptingOneLevel>();

        soloAthMode
            .From<SoloAthAttemptingOneLevel>()
            .When(() => SoloAthContext.IsTimeReached)
            .To<SoloAthEnding>()
            .Or()
            .When(GameGuards.AuthorTimeReached)
            .To<SoloAthWaitingForRespawn>()
            .Or()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.ENDING))
            .To<SoloAthRoundEnding>();


        soloAthMode
            .From<SoloAthWaitingForRespawn>()
            .On<PlayerSpawnedDelegate>(sub => RacingApi.PlayerSpawned += sub, unsub => RacingApi.PlayerSpawned -= unsub)
            .To<SoloAthSkippingLevel>()
            .Or()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.ENDING))
            .To<SoloAthRoundEnding>();

        soloAthMode
            .From<SoloAthSkippingLevel>()
            .When(() => true)
            .To<SoloAthRoundEnding>();
        // ================= ROUND / META =================

        soloAthMode
            .From<SoloAthRoundEnding>()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.PODIUM))
            .To<SoloAthPodium>();

        soloAthMode
            .From<SoloAthPodium>()
            .When(() => PlayerManager.Instance.currentMaster.setupScript.LevelLoadingUI.IsOpen)
            .To<SoloAthLevelLoading>();

        soloAthMode
            .From<SoloAthLevelLoading>()
            .When(() => GameGuards.IsLobbyIn(ZeepkistLobbyState.RACING))
            .To<SoloAthAttemptingOneLevel>();

        return soloAthMode;
    }
}