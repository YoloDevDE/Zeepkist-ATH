using System.Globalization;
using UnityEngine;

namespace AuthorTimeHunting.Util;

public static class ColorExtensions
{
    public static Color Author(this Color _) => new Color(0.7f, 0.21f, 0.58f);
    public static Color Author() => new Color().Author();

    public static Color Gold(this Color _) => new Color(1f, 0.75f, 0f);
    public static Color Gold() => new Color().Gold();

    public static Color GreenSplit(this Color _) => HexToColor("#50E451");
    public static Color GreenSplit() => new Color().GreenSplit();

    public static Color YellowSplit(this Color _) => HexToColor("#EDB227");
    public static Color YellowSplit() => new Color().YellowSplit();

    public static Color Penalty(this Color _) => Color.red;
    public static Color Penalty() => new Color().Penalty();

    public static Color FreeSkip(this Color _) => Color.white;
    public static Color FreeSkip() => new Color().FreeSkip();



    public static Color HexToColor(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return Color.white;
        }

        hex = hex.Replace("#", "").Replace("0x", "");

        if (hex.Length != 6)
        {
            return Color.white;
        }

        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);

        return new Color(r / 255f, g / 255f, b / 255f);
    }
}