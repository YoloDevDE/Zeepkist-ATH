namespace AuthorTimeHunting.Entities;

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