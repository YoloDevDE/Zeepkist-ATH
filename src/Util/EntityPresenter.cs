using ZeepkistNetworking;

namespace AuthorTimeHunting.Util;

public class EntityPresenter
{
    public static string GetLevelNameAndAuthor(OnlineZeeplevel level) => $"{level.Name} by ({level.Author})";
}