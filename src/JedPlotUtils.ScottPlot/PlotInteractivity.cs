using LanguageExt;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotInteractivity(
    Seq<Scatter> Series,
    PlotDecorations Decorations,
    PlotRender PlotRender,
    bool IsTimeSeries
);
