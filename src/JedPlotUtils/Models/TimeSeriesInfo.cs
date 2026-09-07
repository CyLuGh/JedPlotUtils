using System.Collections.Frozen;
using LanguageExt;

namespace JedPlotUtils.Models;

public enum SeriesChartType
{
    Line,
    Area,
    Range
}

public readonly record struct TimeSeriesInfo
{
    public Identifier Identifier { get; }
    public string Label { get; }
    public HashMap<DateOnly, double> Data { get; }
    public SeriesChartType ChartType { get; }
    public HashMap<DateOnly, double> AuxiliaryData { get; }
    public bool IsDerived => Identifier.IsDerived;

    public TimeSeriesInfo(
        string identifier,
        string label,
        IEnumerable<(DateOnly, double)> data,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);

        Identifier = new(identifier, isDerived);
        Label = label;
        Data = data.ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        string identifier,
        string label,
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Range
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentNullException.ThrowIfNull(label, nameof(label));

        Identifier = new(identifier, isDerived);
        Label = label;
        Data = data.ToHashMap();
        AuxiliaryData = auxiliary.ToHashMap();
        ChartType = chartType;
    }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            data,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            data,
            auxiliary,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        IEnumerable<(DateOnly, double)> data,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            Option<string>.None,
            label.Match(l => l, () => string.Empty),
            data,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(IEnumerable<(DateOnly, double)> data, bool isDerived = false)
        : this(Option<string>.None, Option<string>.None, data, isDerived) { }

    public TimeSeriesInfo(
        IEnumerable<(DateOnly, double)> data,
        IEnumerable<(DateOnly, double)> auxiliary,
        bool isDerived = false
    )
        : this(Option<string>.None, Option<string>.None, data, auxiliary, isDerived) { }

    public TimeSeriesInfo(
        string identifier,
        string label,
        DateOnly[] dates,
        double[] values,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);
        ArgumentNullException.ThrowIfNull(dates);
        ArgumentNullException.ThrowIfNull(values);
        ArgumentOutOfRangeException.ThrowIfNotEqual(dates.Length, values.Length);

        Identifier = new(identifier, isDerived);
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
        bool isDerived = false,
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

        Identifier = new(identifier, isDerived);
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
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        double[] auxiliaries,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            identifier.Match(i => i, () => Guid.CreateVersion7().ToString()),
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            auxiliaries,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Line
    )
        : this(
            Option<string>.None,
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            isDerived,
            chartType
        ) { }

    public TimeSeriesInfo(
        Option<string> label,
        DateOnly[] dates,
        double[] values,
        double[] auxiliaries,
        bool isDerived = false,
        SeriesChartType chartType = SeriesChartType.Range
    )
        : this(
            Option<string>.None,
            label.Match(l => l, () => string.Empty),
            dates,
            values,
            auxiliaries,
            isDerived,
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
