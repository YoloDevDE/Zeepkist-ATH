using UnityEngine;

namespace AuthorTimeHunting.States.Ath.StateMachine;

public partial class AthStateMachine
{
	/// <summary>
	///     The only reason a GameObject is involved at all: Unity calls Update on
	///     MonoBehaviours, and the state machine needs a per-frame tick. It owns no logic and
	///     no state beyond the machine it reports back to.
	/// </summary>
	private sealed class AthLoopBehaviour : MonoBehaviour
	{
		private AthStateMachine _owner;

		public void Bind(AthStateMachine owner)
		{
			_owner = owner;
		}

		private void Update()
		{
			if (_owner is not { _timerStarted: true })
			{
				return;
			}

			_owner.OnAthTimerTick();
		}
	}
}
