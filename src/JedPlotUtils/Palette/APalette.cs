using LanguageExt;

namespace JedPlotUtils.Palette;

public abstract record APalette : IPalette
{
    public abstract string Name { get; }
    public Seq<Color> Colors { get; protected set; }
    public Color BackgroundColor { get; } = new(255, 255, 255);

    public virtual Color GetColor(int index)
    {
        int i = index;
        while (i >= Colors.Count)
            i -= Colors.Count;

        return Colors[i];
    }

    // public virtual bool Equals(APalette? other)
    // {
    //     return Name.Equals(other?.Name);
    // }

    // public override int GetHashCode()
    // {
    //     return Name.GetHashCode();
    // }
}
