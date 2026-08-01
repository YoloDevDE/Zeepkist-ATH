using UnityEngine;

namespace AuthorTimeHunting.Service;

public partial class WorkshopDownloadService
{
	/// <summary>
	///     Frame tick for the prefetch check. Same split as AthRunner and
	///     GameStateObserver: Unity only calls Update on MonoBehaviours.
	/// </summary>
	private sealed class PrefetchBehaviour : MonoBehaviour
	{
		private WorkshopDownloadService _owner;

		private void Update()
		{
			_owner?.PrefetchNextLevel();
		}

		public void Bind(WorkshopDownloadService owner)
		{
			_owner = owner;
		}
	}
}