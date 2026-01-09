using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotLayout
{
    public static readonly PlotLayout Default =
        new()
        {
            FigureBackground = Colors.Transparent,
            AxesColor = Colors.Gray,
            GridMajorLineColor = Colors.Gray.WithOpacity(.3)
        };

    public required Color FigureBackground { get; init; }
    public required Color AxesColor { get; init; }
    public required Color GridMajorLineColor { get; init; }
}
