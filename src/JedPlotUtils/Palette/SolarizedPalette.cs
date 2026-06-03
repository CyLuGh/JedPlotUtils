using LanguageExt;

namespace JedPlotUtils.Palette;

public class SolarizedPalette : APalette
{
    public override string Name => "Solarized";

    private int[] _scheme = [15, 10, 13, 8, 12, 11, 14, 9];

    public SolarizedPalette()
    {
        Colors = Seq.create(
            new Color("#002b36"), // base03  - 0
            new Color("#073642"), // base02  - 1
            new Color("#586e75"), // base01  - 2
            new Color("#657b83"), // base00  - 3
            new Color("#839496"), // base0   - 4
            new Color("#93a1a1"), // base1   - 5
            new Color("#eee8d5"), // base2   - 6
            new Color("#fdf6e3"), // base3   - 7
            new Color("#b58900"), // yellow  - 8
            new Color("#cb4b16"), // orange  - 9
            new Color("#dc322f"), // red     - 10
            new Color("#d33682"), // magenta - 11
            new Color("#6c71c4"), // violet  - 12
            new Color("#268bd2"), // blue    - 13
            new Color("#2aa198"), // cyan    - 14
            new Color("#859900") // green   - 15
        );
    }

    public override Color GetColor(int index)
    {
        int i = index;
        while (i >= _scheme.Length)
            i -= _scheme.Length;

        return Colors[_scheme[i]];
    }
}
