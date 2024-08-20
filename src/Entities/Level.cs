namespace AuthorTimeHunting.Entities;

public class Level
{
    public ulong WorkshopId { get; set; }
    public string Name { get; set; }
    public string Author { get; set; }
    public double AuthorTime { get; set; }
    public double GoldTime { get; set; }
    public double WorldRecordTime { get; set; }
    public bool LevelBroken { get; set; }
}