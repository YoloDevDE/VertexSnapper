using System.Globalization;
using UnityEngine;

namespace VertexSnapper.Helper;

public static class ColorUtils
{
    /// <summary>
    ///     Parses a hex RGB string into a UnityEngine.Color.
    ///     Supported formats: "RRGGBB" or "#RRGGBB".
    ///     Returns defaultColor if the string is null, empty, or invalid.
    /// </summary>
    public static Color FromHex(string hex, Color defaultColor = default)
    {
        if (string.IsNullOrWhiteSpace(hex))
        {
            return defaultColor;
        }

        string normalized = hex.Trim();

        if (normalized.StartsWith("#"))
        {
            normalized = normalized.Substring(1);
        }

        if (normalized.Length != 6 || !int.TryParse(normalized, NumberStyles.HexNumber, CultureInfo.InvariantCulture, out int rgb))
        {
            return defaultColor;
        }

        float r = ((rgb >> 16) & 0xFF) / 255f;
        float g = ((rgb >> 8) & 0xFF) / 255f;
        float b = (rgb & 0xFF) / 255f;

        return new Color(r, g, b, 1f);
    }
}