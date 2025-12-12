using JedPlotUtils.ScottPlot;
using LanguageExt;
using ScottPlot;

namespace DemoData;

public readonly record struct ChartTs
{
    public required string Name { get; init; }
    public required Seq<ChartTsItem> Items { get; init; }

    public PlotSeries ToPlotSeries() =>
        new PlotSeries()
        {
            Name = Name,
            Points = Items.Map(x => new Coordinates(x.Date.ToOADate(), x.Value))
        };
}
