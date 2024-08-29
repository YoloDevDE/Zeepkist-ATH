namespace AuthorTimeHunting.Entities;

public class LevelItem
{
    public string Name { get; set; }
    public ulong WorkshopId { get; set; }
    public string FileUid { get; set; }
    public string FileAuthor { get; set; }
    public float ValidationTimeAuthor { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(WorkshopId)}: {WorkshopId}, {nameof(FileUid)}: {FileUid}, {nameof(FileAuthor)}: {FileAuthor}, {nameof(ValidationTimeAuthor)}: {ValidationTimeAuthor}";
    }
}