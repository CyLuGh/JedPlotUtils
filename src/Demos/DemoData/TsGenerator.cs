using HierarchyGrid.Definitions;
using JedPlotUtils.ScottPlot.Common.Components;
using LanguageExt;

namespace DemoData;

public class TsGenerator
{
    private readonly Random _random;

    public Seq<ChartTs> Sample => GenerateSample().ToSeq().Strict();

    public TsGenerator(int? seed = null)
    {
        _random = seed.HasValue ? new(seed.Value) : new();
    }

    public ChartTs GenerateMonthlyTs(string name, DateOnly start, int count) =>
        GenerateTs(name, start, count, d => d.AddMonths(1));

    public ChartTs GenerateTs(
        string name,
        DateOnly start,
        int count,
        Func<DateOnly, DateOnly> increment,
        int? seed = null
    )
    {
        List<ChartTsItem> items = [];
        var current = start;

        while (items.Count < count)
        {
            items.Add(
                new()
                {
                    Date = current.ToDateTime(TimeOnly.MinValue),
                    Value = _random.NextDouble() * 1_000_000
                }
            );
            current = increment(current);
        }

        return new() { Name = name, Items = items.OrderBy(x => x.Date).ToSeq().Strict() };
    }

    public TimeSeriesInfo GenerateTimeSeriesInfo(
        string label,
        DateOnly start,
        int count,
        Func<DateOnly, DateOnly> increment
    )
    {
        var current = start;
        var dates = new List<DateOnly>();
        var values = new List<double>();

        while (dates.Count < count)
        {
            dates.Add(current);
            values.Add(_random.NextDouble() * 1_000_000);
            current = increment(current);
        }

        return new(label, [.. dates], [.. values]);
    }

    private IEnumerable<ChartTs> GenerateSample()
    {
        yield return GenerateTs("Series 1", new DateOnly(2016, 1, 1), 120, d => d.AddMonths(1));
        yield return GenerateTs("Series 2", new DateOnly(2020, 1, 1), 80, d => d.AddMonths(1));
        yield return GenerateTs("Series 3", new DateOnly(2016, 1, 1), 40, d => d.AddMonths(3));
    }
}
