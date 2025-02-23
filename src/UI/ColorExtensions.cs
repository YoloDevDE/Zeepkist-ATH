using System;
using System.Globalization;
using UnityEngine;

namespace AuthorTimeHunting.UI;

public static class ColorExtensions
{
    /// <summary>
    ///     Converts a HEX color string to a Unity Color.
    ///     Supports both formats with and without an alpha channel.
    /// </summary>
    /// <param name="hex">HEX color string (e.g., "#RRGGBB" or "#RRGGBBAA").</param>
    /// <returns>Parsed Color object. Defaults to white if parsing fails.</returns>
    public static Color FromHex(string hex)
    {
        if (string.IsNullOrEmpty(hex))
        {
            return Color.white;
        }

        // Remove '#' if present
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        // Ensure valid length
        if (hex.Length != 6 && hex.Length != 8)
        {
            Debug.LogWarning($"Invalid HEX color length: {hex.Length}. Expected 6 or 8 characters.");
            return Color.white;
        }

        // Parse R, G, B, A components
        try
        {
            byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
            byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
            byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
            byte a = hex.Length == 8
                ? byte.Parse(hex.Substring(6, 2), NumberStyles.HexNumber)
                : (byte)255;

            return new Color32(r, g, b, a);
        }
        catch (Exception ex)
        {
            Debug.LogError($"Failed to parse HEX color: {hex}. Exception: {ex.Message}");
            return Color.white;
        }
    }
}