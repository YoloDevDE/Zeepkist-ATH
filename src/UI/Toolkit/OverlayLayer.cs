using Imui.IO.UGUI;
using UnityEngine;
using Logger = AuthorTimeHunting.Util.Logger;
using Object = UnityEngine.Object;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     Puts the mod's drawing above everything the game has, for as long as it needs to be there.
///     The setup screen exists to hide a lobby being built. Half of that lobby being built is the
///     game's own loading screen, which is a canvas like any other and sorts against ours by
///     order - and ours, being a mod's, was never given one worth having. The result was the
///     trick showing through: a flash of the room list, a flash of the loading screen, and the
///     illusion that ATH is running the show gone with it.
///     So while the setup screen is up, the canvas Imui draws on is lifted to the top of the sort
///     order and put back afterwards. It is put back rather than left there because everything
///     else the mod draws belongs where the game put it - a run panel over the pause menu would
///     be its own bug.
/// </summary>
public class OverlayLayer
{
	/// <summary>As high as a canvas sorts. Nothing in the game asks for this, which is the point.</summary>
	private const int Front = short.MaxValue;

	private Canvas _canvas;

	private bool _raised;

	private bool _searched;

	private int _wasOrder;

	public void Raise()
	{
		if (_raised)
		{
			return;
		}

		Canvas canvas = Find();

		if (canvas == null)
		{
			return;
		}

		_raised = true;
		_wasOrder = canvas.sortingOrder;
		canvas.sortingOrder = Front;
	}

	public void Drop()
	{
		if (!_raised || _canvas == null)
		{
			_raised = false;

			return;
		}

		_canvas.sortingOrder = _wasOrder;
		_raised = false;
	}

	/// <summary>
	///     The canvas Imui renders into, found through the backend component that draws it. Looked
	///     up once and kept: it is created with the game and outlives every scene the mod cares
	///     about.
	/// </summary>
	private Canvas Find()
	{
		if (_searched && _canvas != null)
		{
			return _canvas;
		}

		_searched = true;

		ImuiUnityGUIBackend backend = Object.FindObjectOfType<ImuiUnityGUIBackend>();

		if (backend == null)
		{
			Logger.LogWarning("OverlayLayer: No Imui backend to lift, the game may draw over the setup screen.");

			return null;
		}

		Canvas canvas = backend.GetComponentInParent<Canvas>();

		_canvas = canvas == null ? null : canvas.rootCanvas;

		return _canvas;
	}
}
