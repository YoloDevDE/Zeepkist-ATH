using UnityEngine;

namespace AuthorTimeHunting.Run;

public partial class AthRunner
{
	/// <summary>
	///     The only reason a GameObject is involved at all: Unity calls Update on
	///     MonoBehaviours, and the run needs a per-frame tick. It owns no logic and no state
	///     beyond the runner it reports back to.
	/// </summary>
	private sealed class AthLoopBehaviour : MonoBehaviour
	{
		private AthRunner _owner;

		private void Update()
		{
			if (_owner is not { _timerStarted: true })
			{
				return;
			}

			_owner.OnAthTimerTick();
		}

		public void Bind(AthRunner owner)
		{
			_owner = owner;
		}
	}
}
