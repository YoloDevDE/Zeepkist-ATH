using ZeepkistNetworking;

namespace AuthorTimeHunting.Entities;

public class LevelItem
{
    public string Name { get; set; }
    public ulong WorkshopId { get; set; }
    public string FileUid { get; set; }
    public string FileAuthor { get; set; }
    public float ValidationTimeAuthor { get; set; }
    public ulong AuthorId { get; set; }

    public OnlineZeeplevel ToOnlineZeepLevel()
    {
        return new OnlineZeeplevel
        {
            UID = FileUid,
            WorkshopID = WorkshopId,
            Name = Name,
            Author = FileAuthor,
            played = false
        };
    }

    public override string ToString()
    {
        return $"{nameof(Name)}: {Name}, {nameof(WorkshopId)}: {WorkshopId}, {nameof(FileUid)}: {FileUid}, {nameof(FileAuthor)}: {FileAuthor}, {nameof(ValidationTimeAuthor)}: {ValidationTimeAuthor}, {nameof(AuthorId)}: {AuthorId}";
    }
}