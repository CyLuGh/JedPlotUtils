using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public readonly record struct LegendLayout
{
    public required Orientation Orientation { get; init; }
    public required Edge Edge { get; init; }

    public Color? BackgroundColor { get; init; }
    public Color? BackgroundHatchColor { get; init; }
    public Color? ShadowColor { get; init; }
    public Color? OutlineColor { get; init; }
    public Color? FontColor { get; init; }
    public LineStyle? OutlineStyle { get; init; }
}
