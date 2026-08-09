using System.Collections.Generic;
using UnityEngine;

namespace AuthorTimeHunting.UI.Toolkit;

/// <summary>
///     How far a control is into being hovered, and into being held: 0 for neither, 1 for all the
///     way there, and the frames in between.
///     Imui redraws everything every frame and keeps nothing, so a button that is supposed to
///     take a tenth of a second to light up has nowhere to remember how far along it is. This is
///     that memory, kept per control id rather than per widget, so a widget stays a method with no
///     state of its own.
///     Everything is driven off unscaled time. A paused game still has a menu on top of it, and a
///     button that stops animating when the run pauses reads as a button that stopped working.
/// </summary>
public static class UiMotion
{
	private const float _hoverSeconds = 0.12f;

	private const float _pressSeconds = 0.08f;

	private static readonly Dictionary<uint, float> _values = new();

	private static readonly Dictionary<uint, int> _seen = new();

	private static readonly List<uint> _stale = [];

	private static int _frame = -1;

	public static float Hover(uint id, bool hovered)
	{
		return Ease(Toward(id * 2, hovered, _hoverSeconds));
	}

	public static float Press(uint id, bool held)
	{
		return Ease(Toward(id * 2 + 1, held, _pressSeconds));
	}

	/// <summary>Smoothstep: the ends are soft, so nothing starts or stops with a jolt.</summary>
	public static float Ease(float t)
	{
		return t * t * (3f - 2f * t);
	}

	private static float Toward(uint key, bool active, float seconds)
	{
		Prune();

		_values.TryGetValue(key, out float current);

		float step = Time.unscaledDeltaTime / seconds;
		float next = Mathf.Clamp01(current + (active ? step : -step));

		_values[key] = next;
		_seen[key] = Time.frameCount;

		return next;
	}

	/// <summary>
	///     Forgets whatever was not drawn last frame. Control ids belong to the screen that drew
	///     them, so a page nobody has open would otherwise sit in here for the rest of the session -
	///     and worse, hand its half-lit value to whichever control inherits the id next.
	/// </summary>
	private static void Prune()
	{
		if (_frame == Time.frameCount)
		{
			return;
		}

		_frame = Time.frameCount;
		_stale.Clear();

		Collect();
		Forget();
	}

	private static void Collect()
	{
		foreach (KeyValuePair<uint, int> entry in _seen)
		{
			if (entry.Value >= _frame - 1)
			{
				continue;
			}

			_stale.Add(entry.Key);
		}
	}

	private static void Forget()
	{
		foreach (uint key in _stale)
		{
			_values.Remove(key);
			_seen.Remove(key);
		}
	}
}
