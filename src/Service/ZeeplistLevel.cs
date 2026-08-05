namespace AuthorTimeHunting.Service;

/// <summary>One level entry inside a <see cref="ZeeplistFile" />.</summary>
public class ZeeplistLevel
{
	public string UID { get; set; }
	public string WorkshopID { get; set; }
	public string Name { get; set; }
	public string Author { get; set; }
}
