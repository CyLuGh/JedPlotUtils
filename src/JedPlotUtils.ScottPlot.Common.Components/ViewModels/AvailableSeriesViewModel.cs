using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot.Common.Components.ViewModels;

public partial record AvailableSeriesViewModel : ReactiveRecord
{
    [Reactive]
    public partial bool IsSelected { get; set; }

    public required Scatter Scatter { get; init; }
    public required string Identifier { get; init; }
}
