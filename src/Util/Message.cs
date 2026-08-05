using System.Collections.Generic;

namespace AuthorTimeHunting.Util;

public class Message
{
	public List<string> Lines { get; } = new();

	public override string ToString()
	{
		return $"<#f0f0f0><br>{string.Join("", Lines)}</color>";
	}
}
