using System;

namespace AuthorTimeHunting.Service;

/// <summary>
///     What one backend answered the last time it was asked, and how long it took.
/// </summary>
public class HealthStatus
{
	public HealthStatus(string name)
	{
		Name = name;
	}

	public string Name { get; }

	public bool Checked { get; private set; }

	public bool IsUp { get; private set; }

	public string Detail { get; private set; }

	public long LatencyMs { get; private set; }

	public DateTime CheckedAtUtc { get; private set; }

	public void Report(bool isUp, string detail, long latencyMs)
	{
		Checked = true;
		IsUp = isUp;
		Detail = detail;
		LatencyMs = latencyMs;
		CheckedAtUtc = DateTime.UtcNow;
	}
}
