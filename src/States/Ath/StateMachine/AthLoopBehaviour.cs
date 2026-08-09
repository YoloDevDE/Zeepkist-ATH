using UnityEngine;

namespace AuthorTimeHunting.States.Ath.StateMachine;

/// <summary>
///     The only reason a GameObject is involved at all: Unity calls Update on MonoBehaviours,
///     and <see cref="AthController" /> needs a per-frame tick. It owns no logic and no
///     state beyond the machine it reports back to.
/// </summary>
public sealed class AthLoopBehaviour : MonoBehaviour
{
	private AthController _owner;

	private void Update()
	{
		if (_owner is not { IsTimerRunning: true })
		{
			return;
		}

		_owner.Update();
	}

	public void Bind(AthController owner)
	{
		_owner = owner;
	}
}

