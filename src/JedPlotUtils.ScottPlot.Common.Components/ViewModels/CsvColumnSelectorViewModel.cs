using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ScottPlot.Common.Components.ViewModels;

public partial record CsvColumnSelectorViewModel : ReactiveRecord
{
    [Reactive]
    public partial bool IsSelected { get; set; }

    public required string Content { get; init; }
    public required int Index { get; init; }
    public required int PeriodIndex { get; init; }
    public required Func<string, double> Converter { get; init; }

    public bool IsSelectable => Index != PeriodIndex && IsConvertible;
    public bool IsConvertible => !double.IsNaN(Converter(Content));
}
