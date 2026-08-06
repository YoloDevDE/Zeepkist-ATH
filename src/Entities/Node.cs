namespace AuthorTimeHunting.Entities;

/// <summary>
///     One level as the GraphQL backend returns it.
///     Concrete on purpose: Newtonsoft has to construct it. It was abstract for a while, and
///     the whole level query failed with "Type is an interface or abstract class" on the first
///     node - which was silent, because the fetch falls back to local playlists.
/// </summary>
public class Node
{
	public string Name { get; set; }
	public float ValidationTimeAuthor { get; set; }
	public string FileAuthor { get; set; }
	public string FileUid { get; set; }
	public string WorkshopId { get; set; }

	public string AuthorId { get; set; }
	public float ValidationTimeGold { get; set; }
}
