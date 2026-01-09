using System.Diagnostics.CodeAnalysis;
using LanguageExt;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotInteractivity
{
    public required HashMap<Scatter, string> Series { get; init; }
    public required PlotDecorations Decorations { get; init; }
    public required PlotRender PlotRender { get; init; }
    public required PlotSelection PlotSelection { get; init; }
    public required bool IsTimeSeries { get; init; }

    [SetsRequiredMembers]
    public PlotInteractivity(
        HashMap<Scatter, string> series,
        PlotDecorations decorations,
        PlotRender plotRender,
        PlotSelection plotSelection,
        bool isTimeSeries
    )
    {
        Series = series;
        Decorations = decorations;
        PlotRender = plotRender;
        PlotSelection = plotSelection;
        IsTimeSeries = isTimeSeries;
    }
}

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
