using LanguageExt;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotInteractivity(
    Seq<Scatter> Series,
    PlotDecorations Decorations,
    PlotRender PlotRender,
    PlotSelection PlotSelection,
    bool IsTimeSeries
);

public readonly record struct PlotSelection
{
    public static readonly PlotSelection Default = new() { };

    public PlotSelection() { }

    public SelectionMode SelectionMode { get; init; } = SelectionMode.None;
    public Seq<Scatter> Selection { get; init; }
}

public enum SelectionMode
{
    None,
    Single,
    Multiple
}
