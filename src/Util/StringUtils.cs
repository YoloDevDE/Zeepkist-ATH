namespace AuthorTimeHunting.Util;

public class StringUtils
{
    public static string GetSign(double value)
    {
        if (value < 0)
        {
            return "-";
        }

        if (value == 0)
        {
            return "=";
        }

        return "+";
    }
}