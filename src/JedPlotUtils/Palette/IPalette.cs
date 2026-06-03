using LanguageExt;

namespace JedPlotUtils.Palette;

public interface IPalette
{
    string Name { get; }
    Seq<Color> Colors { get; }

    Color GetColor(int index);
}
