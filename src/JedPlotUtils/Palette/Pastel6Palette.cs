using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Palette;

public record Pastel6Palette : APalette
{
    public override string Name => "Pastel6";

    public static readonly Color Blue = new("#8ecae6");
    public static readonly Color Pink = new("#ffb3c6");
    public static readonly Color Green = new("#b7e4c7");
    public static readonly Color Orange = new("#ffd6a5");
    public static readonly Color Purple = new("#cdb4db");
    public static readonly Color Yellow = new("#f4c95d");

    public Pastel6Palette()
    {
        Colors = Seq.create(Blue, Pink, Green, Orange, Purple, Yellow);
    }
}
