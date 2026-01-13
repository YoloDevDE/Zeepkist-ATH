using ZeepkistNetworking;

namespace AuthorTimeHunting.Entities;

public class LevelItem
{
    public string Name { get; set; }
    public ulong WorkshopId { get; set; }
    public string FileUid { get; set; }
    public string FileAuthor { get; set; }
    public float ValidationTimeAuthor { get; set; }
    public float ValidationTimeGold { get; set; }
    public ulong AuthorId { get; set; }

    public OnlineZeeplevel ToOnlineZeepLevel() => new OnlineZeeplevel
    {
        UID = FileUid,
        WorkshopID = WorkshopId,
        Name = Name,
        Author = FileAuthor,
        played = false
    };
}