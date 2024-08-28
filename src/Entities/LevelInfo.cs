namespace AuthorTimeHunting.Entities;

public class LevelInfo
{
    public string UID { get; set; }
    public ulong WorkshopID { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public bool Played { get; set; } = false; // Assuming 'played' is initially false
}