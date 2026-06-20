using System.Globalization;

namespace Pixory.Services;

/// <summary>The notations Pixory can copy a picked colour in.</summary>
public enum ColorFormat
{
    Hex,
    Rgb,
    Hsl,
}

/// <summary>
/// Turns a plain RGB triple into the user's chosen textual notation. These are
/// the strings that land on the clipboard and show under each palette swatch.
/// </summary>
public static class ColorFormatting
{
    /// <summary>Formats <paramref name="r"/>,<paramref name="g"/>,<paramref name="b"/> in the given notation.</summary>
    public static string Format(byte r, byte g, byte b, ColorFormat format) => format switch
    {
        ColorFormat.Rgb => $"rgb({r}, {g}, {b})",
        ColorFormat.Hsl => FormatHsl(r, g, b),
        _ => $"#{r:X2}{g:X2}{b:X2}",
    };

    /// <summary>A human label for a format, e.g. for the tray menu.</summary>
    public static string DisplayName(ColorFormat format) => format switch
    {
        ColorFormat.Rgb => "RGB",
        ColorFormat.Hsl => "HSL",
        _ => "HEX",
    };

    // Standard RGB -> HSL conversion. Hue in degrees, saturation and lightness
    // as whole percentages, which is what CSS's hsl() expects.
    private static string FormatHsl(byte r, byte g, byte b)
    {
        var rf = r / 255.0;
        var gf = g / 255.0;
        var bf = b / 255.0;

        var max = Math.Max(rf, Math.Max(gf, bf));
        var min = Math.Min(rf, Math.Min(gf, bf));
        var delta = max - min;

        var lightness = (max + min) / 2.0;

        double hue = 0.0;
        double saturation = 0.0;

        if (delta > 0.0)
        {
            saturation = delta / (1.0 - Math.Abs(2.0 * lightness - 1.0));

            if (max == rf)
                hue = ((gf - bf) / delta) % 6.0;
            else if (max == gf)
                hue = (bf - rf) / delta + 2.0;
            else
                hue = (rf - gf) / delta + 4.0;

            hue *= 60.0;
            if (hue < 0.0)
                hue += 360.0;
        }

        return string.Format(CultureInfo.InvariantCulture, "hsl({0}, {1}%, {2}%)",
            Math.Round(hue),
            Math.Round(saturation * 100.0),
            Math.Round(lightness * 100.0));
    }
}
