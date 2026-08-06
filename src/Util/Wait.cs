using System;
using System.Threading;
using System.Threading.Tasks;

namespace AuthorTimeHunting.Util;

/// <summary>
///     Waiting for the game to be in a state it does not announce. Polling rather than
///     listening, because the things worth waiting for here are spread over a disconnect, a
///     scene load and a server round trip, and a poll cannot miss an edge that fired while
///     nothing was subscribed.
/// </summary>
public static class Wait
{
	private static readonly TimeSpan PollInterval = TimeSpan.FromMilliseconds(250);

	/// <summary>Returns false when the timeout ran out first.</summary>
	public static async Task<bool> UntilAsync(Func<bool> ready, TimeSpan timeout, CancellationToken token)
	{
		DateTime deadline = DateTime.UtcNow + timeout;

		while (!ready() && DateTime.UtcNow < deadline)
		{
			await Task.Delay(PollInterval, token);
		}

		return ready();
	}
}
