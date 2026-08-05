using System.Collections.Generic;

namespace AuthorTimeHunting.Service;

/// <summary>JSON model for manual .zeeplist file parsing.</summary>
public class ZeeplistFile
{
	public List<ZeeplistLevel> Levels { get; set; }
}
