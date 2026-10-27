using System.Collections.ObjectModel;
using JDPlus.WS.Models;
using JedPlotUtils.Models;
using LanguageExt;
using LiveChartsCore.Defaults;
using MoreLinq.Extensions;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public static class TimeSeriesInfoExtensions
{
    public static ObservableCollection<DateTimePoint> GetPoints(
        this TimeSeriesInfo tsi,
        Seq<DateOnly> dates
    ) =>
        [
            .. dates.Map(date =>
            {
                var dt = date.ToDateTime(TimeOnly.MinValue);
                return tsi.Data.Find(
                    date,
                    x => new DateTimePoint(dt, x),
                    () => new DateTimePoint(dt, null)
                );
            })
        ];

    public static ObservableCollection<DateTimePoint> GetPoints(this TimeSeriesInfo tsi)
    {
        var collection = new ObservableCollection<DateTimePoint>();

        var freq = tsi.Frequency.Match(f => f, () => DetectFrequency(tsi.Data.Keys));

        if (freq == Frequency.Undefined)
        {
            tsi.Data.ForEach(x =>
                collection.Add(new DateTimePoint(x.Key.ToDateTime(TimeOnly.MinValue), x.Value))
            );
        }
        else
        {
            var increment = GetMonthIncrement(freq);
            var end = tsi.Data.Max(x => x.Key);
            var date = tsi.Data.Min(x => x.Key);

            while (date <= end)
            {
                collection.Add(
                    new DateTimePoint(
                        date.ToDateTime(TimeOnly.MinValue),
                        tsi.Data.Find(date).MatchUnsafe<double?>(x => x, () => null)
                    )
                );
                date = date.AddMonths(increment);
            }
        }

        return collection;
    }

    private static int GetMonthIncrement(Frequency frequency) =>
        frequency switch
        {
            Frequency.Monthly => 1,
            Frequency.BiMonthly => 2,
            Frequency.Quarterly => 3,
            Frequency.QuadriMonthly => 4,
            Frequency.HalfYearly => 6,
            Frequency.Yearly => 12,
            _ => throw new ArgumentOutOfRangeException(nameof(frequency), frequency, null)
        };

    private static Frequency DetectFrequency(IEnumerable<DateOnly> dates)
    {
        var ordered = dates.Distinct().OrderBy(d => d).ToSeq();

        if (ordered.Length < 2)
            return Frequency.Undefined;

        var monthDiffs = ordered
            .Zip(ordered.Skip(1), (a, b) => (b.Year - a.Year) * 12 + (b.Month - a.Month))
            .Where(d => d > 0)
            .ToSeq();

        if (monthDiffs.Length == 0)
            return Frequency.Undefined;

        var periodInMonths = monthDiffs.Aggregate(Gcd);

        return periodInMonths switch
        {
            1 => Frequency.Monthly,
            2 => Frequency.BiMonthly,
            3 => Frequency.Quarterly,
            4 => Frequency.QuadriMonthly,
            6 => Frequency.HalfYearly,
            12 => Frequency.Yearly,
            _ => Frequency.Undefined
        };
    }

    // greatest common divisor for month differences
    private static int Gcd(int a, int b)
    {
        while (b != 0)
        {
            (a, b) = (b, a % b);
        }

        return a;
    }
}
