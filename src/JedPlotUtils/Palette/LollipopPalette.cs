using LanguageExt;

namespace JedPlotUtils.Palette;

internal record LollipopPalette : APalette
{
    public override string Name => "Lollipop";

    public LollipopPalette()
    {
        Colors = Seq.create(
            new Color("#2196f3"), // Blue
            new Color("#f44336"), // Red
            new Color("#9c27b0"), // Purple
            new Color("#4caf50"), // Green
            new Color("#ffc107"), // Amber
            new Color("#cddc39"), // Lime
            new Color("#795548"), // Brown
            new Color("#607d8b"), // Blue Grey
            new Color("#ff5722"), // Deep Orange
            new Color("#3f51b5"), // Indigo
            new Color("#8bc34a"), // Light Green
            new Color("#e91e63"), // Pink
            new Color("#009688"), // Teal
            new Color("#03a9f4"), // Light Blue
            new Color("#00bcd4"), // Cyan
            new Color("#673ab7"), // Deep Purple
            new Color("#ffeb3b"), // Yellow
            new Color("#ff9800"), // Orange
            new Color("#9e9e9e") // Grey
        );
    }
}
