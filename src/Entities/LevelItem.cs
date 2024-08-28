namespace AuthorTimeHunting.Service;

public class LevelItem
{
    public string Name { get; set; }
    public string WorkshopId { get; set; }
    public string FileUid { get; set; }
    public string FileAuthor { get; set; }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(WorkshopId)}: {WorkshopId}, {nameof(FileUid)}: {FileUid}, {nameof(FileAuthor)}: {FileAuthor}";
    }
}