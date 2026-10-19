using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Palette;

public record BankPalette : APalette
{
    public override string Name => "Bank";

    // Blues
    public static readonly Color BlueDark = new("#0092b6");
    public static readonly Color BlueMedium = new("#00afc2");
    public static readonly Color BlueLight = new("#72bbd1");
    public static readonly Color BlueAccent = new("#0087cd");
    public static readonly Color Cyan = new("#00aeef");
    public static readonly Color IceBlue = new("#d7e0e6");

    // Greens
    public static readonly Color Green = new("#88c63d");
    public static readonly Color OliveGreen = new("#778d31");
    public static readonly Color LightGreen = new("#d8e6e0");

    // Magentas
    public static readonly Color Purple = new("#942d6a");
    public static readonly Color Magenta = new("#e21073");
    public static readonly Color Burgundy = new("#b6004b");

    // Warm colors
    public static readonly Color Orange = new("#f37021");
    public static readonly Color Amber = new("#f7941d");
    public static readonly Color Red = new("#ed1c24");
    public static readonly Color Yellow = new("#f1c605");

    // Neutral blue-greys
    public static readonly Color BlueGrey = new("#c0d1d9");
    public static readonly Color LavenderGrey = new("#a5b0d0");

    public BankPalette()
    {
        Colors = Seq.create(
            BlueAccent, // blue
            Magenta, // pink
            Green, // green
            Orange, // orange
            Cyan, // cyan
            Purple, // purple
            Red, // red
            OliveGreen, // olive
            BlueDark, // teal-blue
            Amber, // amber
            Burgundy, // dark magenta
            BlueMedium, // cyan-teal
            Yellow, // bright yellow
            BlueLight, // light blue
            LavenderGrey, // muted blue-grey
            IceBlue, // very light
            LightGreen, // very light green
            BlueGrey // neutral light grey-blue
        );
    }

    private static readonly FrozenSet<Color> DarkColors =
    [
        BlueAccent,
        BlueDark,
        OliveGreen,
        Purple,
        Magenta,
        Burgundy,
        Orange,
        Red,
    ];

    public static Color GetContrastColor(Color color) =>
        DarkColors.Contains(color) ? new Color("#FFFFFF") : new Color("#000000");
}
