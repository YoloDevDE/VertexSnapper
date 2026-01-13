using System.Globalization;
using UnityEngine;

namespace VertexSnapper.Helper;

public static class ColorExtensions
{
    // Bootstrap-like palette (base)
    public static Color Primary => Hex("#0d6efd");

    public static Color Secondary => Hex("#6c757d");

    public static Color Success => Hex("#198754");

    public static Color Danger => Hex("#dc3545");

    public static Color Warning => Hex("#ffc107");

    public static Color Info => Hex("#0dcaf0");

    public static Color Light => Hex("#f8f9fa");

    public static Color Dark => Hex("#212529");

    public static Color Accent => Hex("#6610f2");

    // Soft variants
    public static Color PrimarySoft => Hex("#6ea8fe");

    public static Color SecondarySoft => Hex("#a7acb1");

    public static Color SuccessSoft => Hex("#75b798");

    public static Color DangerSoft => Hex("#ea868f");

    public static Color WarningSoft => Hex("#ffda6a");

    public static Color InfoSoft => Hex("#6edff6");
    public static Color WithAlpha(this Color color, float alpha) => new Color(color.r, color.g, color.b, alpha);

    public static Color WithRed(this Color color, float red) => new Color(red, color.g, color.b, color.a);

    public static Color WithGreen(this Color color, float green) => new Color(color.r, green, color.b, color.a);

    public static Color WithBlue(this Color color, float blue) => new Color(color.r, color.g, blue, color.a);

    // Helper
    private static Color Hex(string hex)
    {
        if (hex.StartsWith("#"))
        {
            hex = hex.Substring(1);
        }

        byte r = byte.Parse(hex.Substring(0, 2), NumberStyles.HexNumber);
        byte g = byte.Parse(hex.Substring(2, 2), NumberStyles.HexNumber);
        byte b = byte.Parse(hex.Substring(4, 2), NumberStyles.HexNumber);
        return new Color(r / 255f, g / 255f, b / 255f, 1f);
    }
}