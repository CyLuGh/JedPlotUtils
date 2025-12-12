using LanguageExt;
using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public record PlotSeries
{
    public required string Name { get; init; }
    public required Seq<Coordinates> Points { get; init; }
}
