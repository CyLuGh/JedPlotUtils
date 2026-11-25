using System.Text;
using HierarchyGrid.Definitions;

namespace JedPlotUtils.Palette;

public readonly record struct Color
{
    public byte A { get; init; }
    public byte R { get; init; }
    public byte G { get; init; }
    public byte B { get; init; }

    public Color(byte a, byte r, byte g, byte b)
    {
        A = a;
        R = r;
        G = g;
        B = b;
    }

    public Color(byte r, byte g, byte b)
        : this(255, r, g, b) { }

    public Color(string hex)
    {
        ArgumentNullException.ThrowIfNull(hex);

        // Remove leading '#'
        if (hex.StartsWith("#"))
            hex = hex[1..];

        if (hex.Length is not (6 or 8))
        {
            throw new ArgumentException(
                "Hex string must be 6 (RRGGBB) or 8 (AARRGGBB) characters long.",
                nameof(hex)
            );
        }

        int start = 0;

        if (hex.Length == 8)
        {
            A = Convert.ToByte(hex.Substring(start, 2), 16);
            start += 2;
        }
        else
        {
            A = 255; // default alpha
        }

        R = Convert.ToByte(hex.Substring(start, 2), 16);
        G = Convert.ToByte(hex.Substring(start + 2, 2), 16);
        B = Convert.ToByte(hex.Substring(start + 4, 2), 16);
    }

    public Color(float c, float m, float y, float k)
    {
        // Clamp values to [0,1]
        c = Math.Clamp(c, 0f, 1f);
        m = Math.Clamp(m, 0f, 1f);
        y = Math.Clamp(y, 0f, 1f);
        k = Math.Clamp(k, 0f, 1f);

        A = 255;

        R = (byte)(255 * (1 - c) * (1 - k));
        G = (byte)(255 * (1 - m) * (1 - k));
        B = (byte)(255 * (1 - y) * (1 - k));
    }

    public override string ToString()
    {
        var sb = new StringBuilder();
        sb.AppendFormat("#{0:X2}", A);
        sb.AppendFormat("{0:X2}", R);
        sb.AppendFormat("{0:X2}", G);
        sb.AppendFormat("{0:X2}", B);
        return sb.ToString();
    }

    public void Deconstruct(out byte a, out byte r, out byte g, out byte b)
    {
        a = A;
        r = R;
        g = G;
        b = B;
    }

    public void Deconstruct(out byte r, out byte g, out byte b)
    {
        r = R;
        g = G;
        b = B;
    }

    public void Deconstruct(out float c, out float m, out float y, out float k)
    {
        float r = R / 255f;
        float g = G / 255f;
        float b = B / 255f;

        k = 1 - Math.Max(r, Math.Max(g, b));

        if (k < 1)
        {
            c = (1 - r - k) / (1 - k);
            m = (1 - g - k) / (1 - k);
            y = (1 - b - k) / (1 - k);
        }
        else
        {
            c = m = y = 0;
        }
    }

    public (byte A, double H, double S, double V) ToHsv()
    {
        double rd = R / 255.0;
        double gd = G / 255.0;
        double bd = B / 255.0;

        double max = Math.Max(rd, Math.Max(gd, bd));
        double min = Math.Min(rd, Math.Min(gd, bd));
        double delta = max - min;

        double h = 0;

        if (delta != 0)
        {
            if (max == rd)
                h = 60 * (((gd - bd) / delta) % 6);
            else if (max == gd)
                h = 60 * (((bd - rd) / delta) + 2);
            else
                h = 60 * (((rd - gd) / delta) + 4);
        }

        if (h < 0)
            h += 360;

        double s = max == 0 ? 0 : delta / max;
        double v = max;

        return (A, h, s, v);
    }

    public static Color Parse(string? s)
    {
        if (string.IsNullOrWhiteSpace(s))
            return new Color(255, 0, 0, 0);

        try
        {
            return new Color(s);
        }
        catch
        {
            return new Color(255, 0, 0, 0);
        }
    }

    public static Color FromRgb(byte r, byte g, byte b) => new(r, g, b);

    public static Color FromArgb(byte a, byte r, byte g, byte b) => new(a, r, g, b);

    public static Color GetBestForegroundColor(Color color) =>
        GetBestForegroundColor(color.R, color.G, color.B);

    public static Color GetBestForegroundColor(byte r, byte g, byte b)
    {
        double Linearize(byte c)
        {
            double v = c / 255.0;
            return v <= 0.04045 ? v / 12.92 : Math.Pow((v + 0.055) / 1.055, 2.4);
        }

        double rl = Linearize(r);
        double gl = Linearize(g);
        double bl = Linearize(b);

        double luminance = 0.2126 * rl + 0.7152 * gl + 0.0722 * bl;

        double contrastWithBlack = (luminance + 0.05) / 0.05;
        double contrastWithWhite = 1.05 / (luminance + 0.05);

        return contrastWithBlack > contrastWithWhite ? Colors.Black : Colors.White;
    }
}

public static class Colors
{
    public static readonly Color Black = new(255, 0, 0, 0);
    public static readonly Color White = new(255, 255, 255, 255);
}

public static class ThemeColorConverter
{
    public static ThemeColor ToThemeColor(this Color color) =>
        new(color.A, color.R, color.G, color.B);
}
