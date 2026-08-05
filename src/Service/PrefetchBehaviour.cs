using UnityEngine;

namespace AuthorTimeHunting.Service;

/// <summary>
///     Frame tick for <see cref="WorkshopDownloadService" />'s prefetch check. Same split as
///     AthLoopBehaviour and <see cref="ObserverBehaviour" />: Unity only calls Update on
///     MonoBehaviours.
/// </summary>
public sealed class PrefetchBehaviour : MonoBehaviour
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
