using UnityEngine;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Gives <see cref="TraceService" /> a frame tick, the same way ObserverBehaviour does for
///     the state observer.
/// </summary>
public sealed class TraceBehaviour : MonoBehaviour
{
	private TraceService _owner;

	private void Update()
	{
		_owner?.Tick();
	}

	public void Bind(TraceService owner)
	{
		_owner = owner;
	}
}
