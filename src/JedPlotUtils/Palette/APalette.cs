using LanguageExt;

namespace JedPlotUtils.Palette;

public abstract class APalette : IPalette
{
    public abstract string Name { get; }
    public Seq<Color> Colors { get; protected set; }

    public virtual Color GetColor(int index)
    {
        int i = index;
        while (i >= Colors.Count)
            i -= Colors.Count;

        return Colors[i];
    }
}
