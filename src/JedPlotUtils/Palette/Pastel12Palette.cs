using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Palette;

public record Pastel12Palette : APalette
{
    public override string Name => "Pastel12";

    public static readonly Color Blue = new("#80b1d3");
    public static readonly Color Orange = new("#fdb462");
    public static readonly Color Green = new("#8dd3c7");
    public static readonly Color Red = new("#fb8072");
    public static readonly Color Purple = new("#bc80bd");
    public static readonly Color Yellow = new("#ffd54f");
    public static readonly Color Teal = new("#4dd0e1");
    public static readonly Color Pink = new("#fccde5");
    public static readonly Color Olive = new("#b3de69");
    public static readonly Color Lavender = new("#bebada");
    public static readonly Color Coral = new("#ffab91");
    public static readonly Color Aqua = new("#b3e5fc");

    public Pastel12Palette()
    {
        Colors = Seq.create(
            Blue,
            Orange,
            Green,
            Red,
            Purple,
            Yellow,
            Teal,
            Pink,
            Olive,
            Coral,
            Lavender,
            Aqua
        );

        DarkColors = [Blue, Red, Purple, Teal];
    }
}
