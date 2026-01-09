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
