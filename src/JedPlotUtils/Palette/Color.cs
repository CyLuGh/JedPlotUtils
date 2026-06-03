using System.Text;

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
}
