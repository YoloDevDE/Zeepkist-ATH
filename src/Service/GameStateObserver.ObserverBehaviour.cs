using UnityEngine;

namespace AuthorTimeHunting.Service;

public partial class GameStateObserver
{
	/// <summary>
	///     Gives the observer a frame tick. Same split as AthStateMachine: Unity only calls
	///     Update on MonoBehaviours, so the behaviour exists purely to call back in.
	/// </summary>
	private sealed class ObserverBehaviour : MonoBehaviour
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
}
