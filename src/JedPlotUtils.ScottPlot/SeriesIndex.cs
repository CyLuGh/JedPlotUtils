using LanguageExt;
using ScottPlot;

namespace JedPlotUtils.ScottPlot;

public readonly record struct SeriesIndex(
    int Index,
    Option<string> Identifier,
    Option<DataPoint> NearestPoint
)
{
    public static readonly SeriesIndex None = new(-1, Option<string>.None, Option<DataPoint>.None);

    public bool IsEmpty => Index == -1 || Identifier.IsNone || NearestPoint.IsNone;
}
