using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotConfiguration
{
    public PlotConfiguration() { }

    public LegendLayout? LegendLayout { get; init; }
    public IPalette? Palette { get; init; }
    public PlotRender PlotRender { get; init; } = PlotRender.Default;
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
