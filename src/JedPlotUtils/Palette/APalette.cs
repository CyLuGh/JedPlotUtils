using System.Collections.Frozen;
using JedPlotUtils.Models;
using LanguageExt;

namespace JedPlotUtils.Palette;

public abstract record APalette : IPalette
{
    public abstract string Name { get; }
    public Seq<Color> Colors { get; protected set; }
    protected FrozenSet<Color> DarkColors { get; set; } = [];

    public Color BackgroundColor { get; } = new(255, 255, 255);

    public virtual Color GetColor(int index) => Colors[Math.Abs(index) % Colors.Count];

    public virtual Color GetColor(TimeSeriesInfo tsi) =>
        tsi.Color.Match(c => c, () => GetColor(tsi.Index));

    public virtual Color GetContrastColor(Color color) =>
        DarkColors.Contains(color) ? new Color("#FFFFFF") : new Color("#000000");

    // public virtual bool Equals(APalette? other)
    // {
    //     return Name.Equals(other?.Name);
    // }

    // public override int GetHashCode()
    // {
    //     return Name.GetHashCode();
    // }
}
