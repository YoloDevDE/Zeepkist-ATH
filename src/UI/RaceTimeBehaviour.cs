using UnityEngine;

namespace AuthorTimeHunting.UI;

/// <summary>
///     The frame hook for <see cref="RaceTimeDisplay" />. Separate because RaceTimeDisplay is
///     a plain service and only a MonoBehaviour gets a LateUpdate - the same split
///     AthStateMachine uses.
/// </summary>
public sealed class RaceTimeBehaviour : MonoBehaviour
{
	private RaceTimeDisplay _owner;

	private void LateUpdate()
	{
		_owner?.LateUpdate();
	}

	public void Bind(RaceTimeDisplay owner)
	{
		_owner = owner;
	}
}
