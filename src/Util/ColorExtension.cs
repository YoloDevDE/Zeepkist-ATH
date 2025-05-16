using UnityEngine;

namespace AuthorTimeHunting.Util;

public static class ColorExtension
{
    public static Color text_Author => Color.white;
    public static Color bg_Author => new Color(0.5f, 0f, 0.5f);

    public static Color text_Gold => Color.black;
    public static Color bg_Gold => new Color(1f, 0.84f, 0f);


    public static Color text_Penalty => Color.white;
    public static Color bg_Penalty => Color.red;

    public static Color text_Freeskip => Color.black;
    public static Color bg_Freeskip => Color.white;

    public static Color text_UltraPenalty => new Color(0.8f, 0f, 0f);
    public static Color bg_UltraPenalty => new Color(0.4f, 0f, 0f);
}