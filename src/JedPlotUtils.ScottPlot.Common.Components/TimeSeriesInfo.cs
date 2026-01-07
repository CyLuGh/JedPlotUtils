using LanguageExt;
using ScottPlot;

namespace JedPlotUtils.ScottPlot.Common.Components;

public readonly record struct TimeSeriesInfo
{
    public string Identifier { get; }
    public string Label { get; }
    public HashMap<DateOnly, double> Data { get; }

    public TimeSeriesInfo(string identifier, string label, IEnumerable<(DateOnly, double)> data)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentNullException.ThrowIfNull(label, nameof(label));

        Identifier = identifier;
        Label = label;
        Data = data.ToHashMap();
    }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        IEnumerable<(DateOnly, double)> data
    )
        : this(
            identifier.Match(i => i, () => Guid.NewGuid().ToString()),
            label.Match(l => l, () => string.Empty),
            data
        ) { }

    public TimeSeriesInfo(Option<string> label, IEnumerable<(DateOnly, double)> data)
        : this(Option<string>.None, label.Match(l => l, () => string.Empty), data) { }

    public TimeSeriesInfo(IEnumerable<(DateOnly, double)> data)
        : this(Option<string>.None, Option<string>.None, data) { }

    public TimeSeriesInfo(string identifier, string label, DateOnly[] dates, double[] values)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier, nameof(identifier));
        ArgumentNullException.ThrowIfNull(label, nameof(label));
        ArgumentNullException.ThrowIfNull(dates, nameof(dates));
        ArgumentNullException.ThrowIfNull(values, nameof(values));
        ArgumentOutOfRangeException.ThrowIfNotEqual(dates.Length, values.Length);

        Identifier = identifier;
        Label = label;
        Data = Enumerable.Range(0, dates.Length).Select(i => (dates[i], values[i])).ToHashMap();
    }

    public TimeSeriesInfo(
        Option<string> identifier,
        Option<string> label,
        DateOnly[] dates,
        double[] values
    )
        : this(
            identifier.Match(i => i, () => Guid.NewGuid().ToString()),
            label.Match(l => l, () => string.Empty),
            dates,
            values
        ) { }

    public TimeSeriesInfo(Option<string> label, DateOnly[] dates, double[] values)
        : this(Option<string>.None, label.Match(l => l, () => string.Empty), dates, values) { }

    public TimeSeriesInfo(DateOnly[] dates, double[] values)
        : this(Option<string>.None, Option<string>.None, dates, values) { }

    public PlotSeries PlotSeries =>
        new()
        {
            Identifier = Identifier,
            Name = Label,
            Points = Data.Map(p => new Coordinates(
                    p.Key.ToDateTime(TimeOnly.MinValue).ToOADate(),
                    p.Value
                ))
                .OrderBy(c => c.X)
                .ToSeq()
        };
}
