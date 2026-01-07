using System;
using System.Collections.Frozen;
using System.Text;

namespace JedPlotUtils.ScottPlot.Common.Components;

public static class TimeSeriesInfoExtensions
{
    public static FrozenSet<DateOnly> GetDates(this IEnumerable<TimeSeriesInfo> infos) =>
        infos.Map(i => i.Data.Keys).Flatten().Distinct().ToFrozenSet();
}
