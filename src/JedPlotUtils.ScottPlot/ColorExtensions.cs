using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public static class ColorExtensions
{
    public static Color GetMatchingForegroundColor(this Color c) =>
        c.IsBright() ? Colors.Black : Colors.White;

    public static bool IsBright(this Color c) => c.PerceivedBrightness() > 130;

    public static int PerceivedBrightness(this Color c) =>
        (int)Math.Sqrt((c.R * c.R * .299) + (c.G * c.G * .587) + (c.B * c.B * .114));

    public static Color MixColors(this IEnumerable<Color> colors)
    {
        var seq = colors.ToSeq();
        if (seq.IsEmpty)
            return Colors.Transparent;

        Color result = seq[0];
        for (int i = 1; i < seq.Length; i++)
            result = result.MixedWith(seq[i], .5);

        return result;
    }
}
