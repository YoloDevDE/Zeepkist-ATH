using AuthorTimeHunting.Entities;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPausing(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private bool _hasShownMedalRoundOverText;
	// Constructor


	// Properties

	// Public Methods
	public override void Enter()
	{
		_hasShownMedalRoundOverText = false;
	}

	public override void Execute()
	{
		if (!_hasShownMedalRoundOverText)
		{
			PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer?.CurrentResult;
			double lastRunTime = AthStateMachine.Ctx.LastRunTime > 0
				? AthStateMachine.Ctx.LastRunTime
				: currentResult?.Time ?? -1;
			bool hasMedalToShow =
				AthStateMachine.Ctx.LastRunMedalStatus is Level.LevelStatus.AUTHOR or Level.LevelStatus.GOLD;

			if (currentResult != null && hasMedalToShow && lastRunTime >= 0)
			{
				MedalTextHelper.SetMedalProgressText(AthStateMachine.Ctx.CurrentLevel, lastRunTime,
					AthStateMachine.Ctx.LastRunMedalWasNew);
				_hasShownMedalRoundOverText = true;
			}
		}

		AthStateMachine.Ctx.ResetRetries();
		AthStateMachine.SetServerMessage(true);
	}

	public override void OnRoundEnded()
	{
		StateMachine.TransitionTo(new StateAthEvaluateSkip(AthStateMachine));
	}

	// Private Methods
	public override void OnAthTimerTick()
	{
		AthStateMachine.SetServerMessage(true);
		// ZeepkistNetwork.PlayerList
		// ZeepTourney.TourneyApi.startNamedTournament("Pengkob Ranked Mod {rankedId}");
	}


	public override void OnRoundStarted()
	{
		StateMachine.TransitionTo(new StateAthOnARun(AthStateMachine));
	}

	public override void OnPhotoModeEntered()
	{
		OnRoundStarted();
	}

	public override void OnPlayerSpawned()
	{
		MedalTextHelper.ClearMedalText();
	}
}