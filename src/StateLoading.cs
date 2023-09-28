using System;
using ZeepSDK.Racing;

namespace AuthorTimeHunting;

public class StateLoading : ChallengeState
{
    private readonly Challenge _challenge;


    public StateLoading(Challenge challenge) : base(challenge)
    {
        _challenge = challenge;
    }

    public override void Enter()
    {
        RacingApi.LevelLoaded += OnLevelLoaded;
        _challenge.LoadingTimeStart = DateTime.Now;
    }

    public override void Exit()
    {
        RacingApi.LevelLoaded -= OnLevelLoaded;
        _challenge.LoadingTimeEnd = DateTime.Now;
        _challenge.LoadingTime += _challenge.LoadingTimeEnd - _challenge.LoadingTimeStart;
        _challenge.EndTime = _challenge.EndTime.Add(_challenge.LoadingTimeEnd - _challenge.LoadingTimeStart);
        _challenge.GoldSkip = false;
    }


    public void OnLevelLoaded()
    {
        _challenge.SwitchState(new StateRunning(_challenge));
    }
}