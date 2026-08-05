using UnityEngine;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Gives <see cref="GameStateObserver" /> a frame tick. Same split as AthLoopBehaviour:
///     Unity only calls Update on MonoBehaviours, so the behaviour exists purely to call back
///     in.
/// </summary>
public sealed class ObserverBehaviour : MonoBehaviour
{
	private GameStateObserver _owner;

	private void Update()
	{
		_owner?.Tick();
	}

	public void Bind(GameStateObserver owner)
	{
		_owner = owner;
	}
}
