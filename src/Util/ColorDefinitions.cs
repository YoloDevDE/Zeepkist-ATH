using System.Globalization;
using UnityEngine;

namespace AuthorTimeHunting.Util;

public static class ColorDefinitions
{
	public static Color Author => new(0.7f, 0.21f, 0.58f);


	public static Color Gold => new(01f, 0.75f, 0f);

	public static Color GreenSplit => HexToColor("#50E451");
	public static Color YellowSplit => HexToColor("#EDB227");


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
