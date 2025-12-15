using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotConfiguration
{
    public PlotConfiguration() { }

    public LegendLayout? LegendLayout { get; init; }
    public PlotLayout PlotLayout { get; init; } = PlotLayout.Default;
    public IPalette? Palette { get; init; }
    public PlotRender PlotRender { get; init; } = PlotRender.Default;
}

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

public readonly record struct PlotRender
{
    public static readonly PlotRender Default =
        new()
        {
            UseDateTimeAxis = true,
            InteractivityMode = InteractivityMode.AllSeries,
            XFormatter = d => $"{DateTime.FromOADate(d):yyyy-MM-dd}",
            YFormatter = d => $"{d:N}"
        };

    public bool UseDateTimeAxis { get; init; }
    public InteractivityMode InteractivityMode { get; init; }
    public required Func<double, string> XFormatter { get; init; }
    public required Func<double, string> YFormatter { get; init; }
}
