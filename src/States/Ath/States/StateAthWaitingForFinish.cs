using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

/// <summary>
///     The level is being driven. The clock runs here and nowhere else.
///     A finish only ends the state when it is one the game would score - every checkpoint
///     passed. Crossing the line short of that is a dnf, and a dnf costs the same as never
///     having reached the finish at all: the level's clock keeps running and the player gets
///     another attempt.
/// </summary>
public class StateAthWaitingForFinish(AthController controller) : AthState(controller)
{
	public override void Enter()
	{
		AthController.Ctx.CurrentLevel.Attempt++;

		if (!AthController.Ctx.IsPaused)
		{
			AthController.Ctx.CurrentLevel.ResumeTiming();
		}

		Update();
	}

	public override void Exit()
	{
		AthController.Ctx.CurrentLevel.PauseTiming();
	}

	public override void OnRoundEnded()
	{
		Controller.TransitionTo(new StateAthSkippingLevel(AthController));
	}

	public override void OnCrossedFinishLine(float time)
	{
		if (!GameStateObserver.AllCheckpointsPassed)
		{
			Logger.LogInfo(
				"StateAthWaitingForFinish: Finish crossed without every checkpoint, the level keeps running.");
			FrogNotification.Warn("Checkpoint missed - that run does not count");

			return;
		}

		ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
		PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
		Level currentLevel = AthController.Ctx.CurrentLevel;

		if (currentResult == null)
		{
			Controller.TransitionTo(new StateAthWaitingForNextRun(AthController));
			return;
		}

		LevelStatus previousStatus = currentLevel.Status;

		currentLevel.RecordRun(currentResult.Time, GameStateObserver.CurrentSplits);
		LevelStatus runMedalStatus = ResolveRunMedalStatus(currentResult.Time, currentLevel);

		AthController.Ctx.LastRunTime = currentResult.Time;
		AthController.Ctx.LastRunMedalStatus = runMedalStatus;
		AthController.Ctx.LastRunMedalWasNew = GetMedalRank(runMedalStatus) > GetMedalRank(previousStatus);

		bool wasGoldMedalAcquiredBeforeRun = GetMedalRank(previousStatus) >= GetMedalRank(LevelStatus.Gold);

		if (currentLevel.Status == LevelStatus.Author)
		{
			Controller.TransitionTo(new StateAthWaitingForRespawn(AthController));
			return;
		}

		if (runMedalStatus == LevelStatus.Gold && !wasGoldMedalAcquiredBeforeRun)
		{
			FrogNotification.Gold("Gold medal claimed!<br>You can now skip without penalty");
		}

		Controller.TransitionTo(new StateAthWaitingForNextRun(AthController));
	}

	public override void OnRoundStarted()
	{
		AthController.Ctx.CurrentLevel.Attempt++;
		Update();
	}

	public override void Update()
	{
		if (AthController.Ctx.IsTimeOver())
		{
			Controller.TransitionTo(new StateAthStopping(AthController));
			return;
		}

		if (AthController.Ctx.CheckAndNotifyTimeRunningLow())
		{
			FrogNotification.Info("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", 10f);
		}
	}

	private static int GetMedalRank(LevelStatus status)
	{
		return status switch
		{
			LevelStatus.Author => 2, LevelStatus.Gold => 1, _ => 0
		};
	}

	private static LevelStatus ResolveRunMedalStatus(float runTime, Level level)
	{
		if (runTime <= level.AuthorTime)
		{
			return LevelStatus.Author;
		}

		if (runTime <= level.GoldTime)
		{
			return LevelStatus.Gold;
		}

		return LevelStatus.Unknown;
	}
}
