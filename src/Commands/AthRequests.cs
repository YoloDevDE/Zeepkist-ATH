using System;

namespace AuthorTimeHunting.Commands;

/// <summary>
///     What the UI asks the run to do: start, stop, restart, or write the level off as broken.
///     These four used to be chat commands, and the master states subscribed to the command
///     objects themselves. The commands are gone - ATH has one, /ath - but the seam they
///     provided is still the right one: a button knows what it wants, not who is listening,
///     and the master state machine is the only thing that knows whether a run exists.
/// </summary>
public static class AthRequests
{
	public static event Action StartRequested;

	public static event Action StopRequested;

	public static event Action RestartRequested;

	public static event Action SkipBrokenRequested;

	public static void Start()
	{
		StartRequested?.Invoke();
	}

	public static void Stop()
	{
		StopRequested?.Invoke();
	}

	public static void Restart()
	{
		RestartRequested?.Invoke();
	}

	public static void SkipBroken()
	{
		SkipBrokenRequested?.Invoke();
	}
}
