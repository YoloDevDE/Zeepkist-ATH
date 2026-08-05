using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthWaitingForFinish(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		// A run resumed while the player paused ATH must not restart the clock.
		if (!AthStateMachine.Ctx.IsPaused)
		{
			AthStateMachine.Ctx.CurrentLevel.ResumeTiming();
		}

		Update();
	}

	public override void Exit()
	{
		AthStateMachine.Ctx.CurrentLevel.PauseTiming();
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthSkippingLevel(AthStateMachine));
	}

	public override void OnCrossedFinishLine(float time)
	{
		ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
		PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
		Level currentLevel = AthStateMachine.Ctx.CurrentLevel;

		if (currentResult == null)
		{
			StateMachine.TransitionTo(new StateAthWaitingForNextRun(AthStateMachine));
			return;
		}

		LevelStatus previousStatus = currentLevel.Status;

		currentLevel.PersonalBestTime = currentResult.Time;
		LevelStatus runMedalStatus = ResolveRunMedalStatus(currentResult.Time, currentLevel);

		AthStateMachine.Ctx.LastRunTime = currentResult.Time;
		AthStateMachine.Ctx.LastRunMedalStatus = runMedalStatus;
		AthStateMachine.Ctx.LastRunMedalWasNew = GetMedalRank(runMedalStatus) > GetMedalRank(previousStatus);

		bool wasGoldMedalAcquiredBeforeRun = GetMedalRank(previousStatus) >= GetMedalRank(LevelStatus.GOLD);

		if (currentLevel.Status == LevelStatus.AUTHOR)
		{
			StateMachine.TransitionTo(new StateAthWaitingForRespawn(AthStateMachine));
			return;
		}

		if (runMedalStatus == LevelStatus.GOLD && !wasGoldMedalAcquiredBeforeRun)
		{
			ToastNotification.Gold("Gold medal claimed!<br>You can now skip without penalty");
		}

		StateMachine.TransitionTo(new StateAthWaitingForNextRun(AthStateMachine));
	}

	public override void OnRoundStarted()
	{
		AthStateMachine.Ctx.CurrentLevel.Attempt++;
		Update();
	}

	public override void Update()
	{
		if (AthStateMachine.Ctx.IsTimeOver())
		{
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
			return;
		}

		AthStateMachine.SetServerMessage(false);

		if (AthStateMachine.Ctx.CheckAndNotifyTimeRunningLow())
		{
			ToastNotification.Info("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", 10f);
		}
	}

	private static int GetMedalRank(LevelStatus status)
	{
		return status switch
		{
			LevelStatus.AUTHOR => 2, LevelStatus.GOLD => 1, _ => 0
		};
	}

	private static LevelStatus ResolveRunMedalStatus(float runTime, Level level)
	{
		if (runTime <= level.AuthorTime)
		{
			return LevelStatus.AUTHOR;
		}

		if (runTime <= level.GoldTime)
		{
			return LevelStatus.GOLD;
		}

		return LevelStatus.UNKNOWN;
	}
}
