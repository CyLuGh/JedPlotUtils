using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Palette;

public record Professional10Palette : APalette
{
    public override string Name => "Professional10";

    public static readonly Color Blue = new("#4E79A7");
    public static readonly Color Orange = new("#F28E2B");
    public static readonly Color Red = new("#E15759");
    public static readonly Color Teal = new("#76B7B2");
    public static readonly Color Green = new("#59A14F");
    public static readonly Color Yellow = new("#EDC948");
    public static readonly Color Purple = new("#B07AA1");
    public static readonly Color Pink = new("#FF9DA7");
    public static readonly Color Brown = new("#9C755F");
    public static readonly Color Grey = new("#BAB0AC");

    public Professional10Palette()
    {
        Colors = Seq.create(Blue, Orange, Red, Teal, Green, Yellow, Purple, Pink, Brown, Grey);
        DarkColors = [Blue, Orange, Red, Teal, Green, Purple, Brown];
    }
}
