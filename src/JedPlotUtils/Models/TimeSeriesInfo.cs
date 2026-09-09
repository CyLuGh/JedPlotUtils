using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Models;

public enum SeriesChartType
{
    Line,
    Area,
    Range
}

public enum Level
{
    Primary,
    Secondary,
    Tertiary
}

public readonly record struct TimeSeriesInfo
{
    public Identifier Identifier { get; }
    public string Label { get; init; }
    public HashMap<DateOnly, double> Data { get; }
    public SeriesChartType ChartType { get; }
    public HashMap<DateOnly, double> AuxiliaryData { get; }
    public Level Level => Identifier.Level;
    public bool IsDerived => Level != Level.Primary;
    public int Index { get; init; }

    public TimeSeriesInfo(
        string identifier,
        string label,
        IEnumerable<(DateOnly, double)> data,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);

        Identifier = new(identifier, level);
        Label = label;
        Data = data.ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        string identifier,
        string label,
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Range
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentNullException.ThrowIfNull(label, nameof(label));

        Identifier = new(identifier, level);
        Label = label;
        Data = data.ToHashMap();
        AuxiliaryData = auxiliary.ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            data,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            data,
            auxiliary,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(Option<string>.None, label.Match(l => l, () => string.Empty), data, level, chartType)
    { }

    public TimeSeriesInfo(IEnumerable<(DateOnly, double)> data, Level level = Level.Primary)
        : this(Option<string>.None, Option<string>.None, data, level) { }

    public TimeSeriesInfo(
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        Level level = Level.Primary
    )
        : this(Option<string>.None, Option<string>.None, data, auxiliary, level) { }

    public TimeSeriesInfo(
        string identifier,
        string label,
        DateOnly[] dates,
        double[] values,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNotEqual(dates.Length, values.Length);

        Identifier = new(identifier, level);
        Label = label;
        Data = Enumerable.Range(0, dates.Length).Select(i => (dates[i], values[i])).ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        string identifier,
        string label,
        DateOnly[] dates,
        double[] values,
        double[] auxiliaries,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Range
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentNullException.ThrowIfNull(auxiliaries);
        ArgumentOutOfRangeException.ThrowIfNotEqual(dates.Length, values.Length);
        ArgumentOutOfRangeException.ThrowIfNotEqual(dates.Length, auxiliaries.Length);

        Identifier = new(identifier, level);
        Label = label;
        Data = Enumerable.Range(0, dates.Length).Select(i => (dates[i], values[i])).ToHashMap();
        AuxiliaryData = Enumerable
            .Range(0, dates.Length)
            .Select(i => (dates[i], auxiliaries[i]))
            .ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        double[] auxiliaries,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            auxiliaries,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            Option<string>.None,
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        double[] auxiliaries,
        Level level = Level.Primary,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            Option<string>.None,
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            auxiliaries,
            level,
            chartType
        ) { }

    public TimeSeriesInfo(DateOnly[] dates, double[] values)
        : this(Option<string>.None, Option<string>.None, dates, values) { }
}

public static class TimeSeriesInfoExtensions
{
    public static FrozenSet<DateOnly> GetDates(this IEnumerable<TimeSeriesInfo> infos) =>
        infos.Map(i => i.Data.Keys).Flatten().Distinct().ToFrozenSet();
}
