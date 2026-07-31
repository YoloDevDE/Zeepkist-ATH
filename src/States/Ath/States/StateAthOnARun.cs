using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.UI;
using AuthorTimeHunting.Util;
using UnityEngine;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthOnARun(AthStateMachine stateMachine) : AthState(stateMachine)
{
	public override void Enter()
	{
		// A run resumed while the player paused ATH must not restart the clock.
		if (!AthStateMachine.Ctx.IsPaused)
		{
			AthStateMachine.Ctx.CurrentLevel.ResumeTiming();
		}
	}

	public override void Execute()
	{
		OnAthTimerTick();
		AthStateMachine.Show(AthStateMachine.Ctx.Messages.OnARun());
	}

	public override void Exit()
	{
		AthStateMachine.Ctx.CurrentLevel.PauseTiming();
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthEvaluateSkip(AthStateMachine));
	}

	public override void OnCrossedFinishLine(float time)
	{
		ZeepkistNetworkPlayer networkPlayer = ZeepkistNetwork.LocalPlayer;
		PlayerBase.Result currentResult = networkPlayer?.CurrentResult;
		Level currentLevel = AthStateMachine.Ctx.CurrentLevel;

		if (currentResult == null)
		{
			StateMachine.TransitionTo(new StateAthPausing(AthStateMachine));
			return;
		}

		Level.LevelStatus previousStatus = currentLevel.Status;

		currentLevel.PersonalBestTime = currentResult.Time;
		Level.LevelStatus runMedalStatus = ResolveRunMedalStatus(currentResult.Time, currentLevel);

		AthStateMachine.Ctx.LastRunTime = currentResult.Time;
		AthStateMachine.Ctx.LastRunMedalStatus = runMedalStatus;
		AthStateMachine.Ctx.LastRunMedalWasNew = GetMedalRank(runMedalStatus) > GetMedalRank(previousStatus);

		bool wasGoldMedalAcquiredBeforeRun = GetMedalRank(previousStatus) >= GetMedalRank(Level.LevelStatus.GOLD);

		if (currentLevel.Status == Level.LevelStatus.AUTHOR)
		{
			StateMachine.TransitionTo(new StateAthWaitingForRespawn(AthStateMachine));
			return;
		}

		if (runMedalStatus == Level.LevelStatus.GOLD && !wasGoldMedalAcquiredBeforeRun)
		{
			Overlay.Notify("Gold medal claimed!<br>You can now skip without penalty", HudPalette.Default, 5f);
			Overlay.ShowBanner(MedalBanner.Message("New Medal Claimed: Gold", 5f));
			AthStateMachine.Show(AthStateMachine.Ctx.Messages.GoldMedalClaimed());
		}

		StateMachine.TransitionTo(new StateAthPausing(AthStateMachine));
		AthStateMachine.Show(AthStateMachine.Ctx.Messages.CrossedFinishLine());
	}

	public override void OnRoundStarted()
	{
		Overlay.ClearBanner();
		AthStateMachine.Ctx.CurrentLevel.Attempt++;
		Execute();
	}

	public override void OnAthTimerTick()
	{
		if (AthStateMachine.Ctx.IsTimeOver())
		{
			StateMachine.TransitionTo(new StateAthStopping(AthStateMachine));
			return;
		}

		AthStateMachine.SetServerMessage(false);

		if (AthStateMachine.Ctx.CheckAndNotifyTimeRunningLow())
		{
			Overlay.Notify("<b>Time is running low!</b><br>A 'Penalty-Skip' will end the run!", HudPalette.Default, 10f);
		}
	}

	private static int GetMedalRank(Level.LevelStatus status)
	{
		return status switch
		{
			Level.LevelStatus.AUTHOR => 2, Level.LevelStatus.GOLD => 1, _ => 0
		};
	}

	private static Level.LevelStatus ResolveRunMedalStatus(float runTime, Level level)
	{
		if (runTime <= level.AuthorTime)
		{
			return Level.LevelStatus.AUTHOR;
		}

		if (runTime <= level.GoldTime)
		{
			return Level.LevelStatus.GOLD;
		}

		return Level.LevelStatus.UNKNOWN;
	}
}