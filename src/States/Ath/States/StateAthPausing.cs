using AuthorTimeHunting.Entities;
using AuthorTimeHunting.Service;
using AuthorTimeHunting.States.Ath.StateMachine;
using AuthorTimeHunting.Util;
using ZeepkistClient;
using ZeepkistNetworking;

namespace AuthorTimeHunting.States.Ath.States;

public class StateAthPausing(AthStateMachine stateMachine) : AthState(stateMachine)
{
	private bool _hasShownMedal;
	// Constructor


	// Properties

	// Public Methods
	public override void Enter()
	{
		_hasShownMedal = false;
	}

	public override void Execute()
	{
		if (!_hasShownMedal)
		{
			PlayerBase.Result currentResult = ZeepkistNetwork.LocalPlayer?.CurrentResult;
			double lastRunTime = AthStateMachine.Ctx.LastRunTime > 0
				? AthStateMachine.Ctx.LastRunTime
				: currentResult?.Time ?? -1;
			bool hasMedalToShow =
				AthStateMachine.Ctx.LastRunMedalStatus is Level.LevelStatus.AUTHOR or Level.LevelStatus.GOLD;

			if (currentResult != null && hasMedalToShow && lastRunTime >= 0)
			{
				_hasShownMedal = true;
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

	/// <summary>
	///     Photo mode is treated as "the player is driving again", because RoundStarted and
	///     this are the only two ways back into a run from the pause screen.
	///     It is only that during a race, though. Entering photo mode on the podium or while
	///     the next level loads used to restart the run clock and bill the player for time
	///     they spent looking at a screenshot.
	/// </summary>
	public override void OnPhotoModeEntered()
	{
		if (!GameStateObserver.IsRacing)
		{
			Logger.LogInfo("StateAthPausing: Photo mode entered outside a running race, keeping the clock paused.");
			return;
		}

		OnRoundStarted();
	}

	public override void OnPlayerSpawned()
	{
	}
}