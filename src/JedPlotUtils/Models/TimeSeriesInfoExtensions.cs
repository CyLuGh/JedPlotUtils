using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using CsvHelper;
using CsvHelper.Configuration;

namespace JedPlotUtils.Models;

public static class TimeSeriesInfoExtensions
{
    extension(IEnumerable<TimeSeriesInfo> infos)
    {
        public FrozenSet<DateOnly> GetDates() =>
            infos.Map(i => i.Data.Keys).Flatten().Distinct().ToFrozenSet();

        public string ToJson(bool indented = false) =>
            JsonSerializer.Serialize(infos, new JsonSerializerOptions() { WriteIndented = indented });

        public string ToCsv(string separator = ",", IFormatProvider? formatProvider = null)
        {
            var seq = infos.ToSeq();
            var sb = new StringBuilder();

            formatProvider ??= new CultureInfo("en-US");
            sb.AppendLine("Period" + separator + string.Join(separator, seq.Map(tsi => tsi.Label)));
            foreach (var period in seq.GetDates().Items.OrderBy(x => x))
            {
                sb.AppendLine(period.ToString("yyyy-MM-dd") + separator + string.Join(separator,
                    seq.Map(tsi => tsi.Data.Find(period, d => d.ToString(formatProvider), () => string.Empty))));
            }

            return sb.ToString();
        }

        public byte[] ToBytes() => Encoding.UTF8.GetBytes(infos.ToJson());
    }

    private static readonly CultureInfo[] CandidateCultures =
    [
        CultureInfo.InvariantCulture,
        CultureInfo.GetCultureInfo("en-US"),
        CultureInfo.GetCultureInfo("en-GB"),
        CultureInfo.GetCultureInfo("fr-FR"),
        CultureInfo.GetCultureInfo("nl-BE"),
        CultureInfo.GetCultureInfo("de-DE")
    ];

    private static CultureInfo GuessCulture(
        IEnumerable<string[]> rows,
        int firstNumericColumn = 1)
    {
        return CandidateCultures
            .Select(culture => new
            {
                Culture = culture,
                Score = rows.Sum(row =>
                    row.Skip(firstNumericColumn)
                        .Count(value =>
                            double.TryParse(
                                value,
                                NumberStyles.Float | NumberStyles.AllowThousands,
                                culture,
                                out _)))
            })
            .OrderByDescending(x => x.Score)
            .First()
            .Culture;
    }

    private static DateOnly? ParseDate(string value)
    {
        string[] formats =
        [
            "yyyy-MM-dd",
            "yyyy/MM/dd",
            "dd/MM/yyyy",
            "MM/dd/yyyy",
            "dd-MM-yyyy",
            "MM-dd-yyyy"
        ];

        return DateOnly.TryParseExact(
            value,
            formats,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out var date)
            ? date
            : null;
    }

    extension(string text)
    {
        public TimeSeriesInfo[] ToTimeSeriesInfo() => JsonSerializer.Deserialize<TimeSeriesInfo[]>(text) ?? [];

        public TimeSeriesInfo[] FromCsv()
        {
            // First pass: detect delimiter and read raw strings
            using var delimiterReader = new StringReader(text);

            var detectConfig = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                DetectDelimiter = true,
                DetectDelimiterValues = [",", ";", "\t", "|"],
                HasHeaderRecord = true
            };

            using var detectCsv = new CsvReader(delimiterReader, detectConfig);

            detectCsv.Read();
            detectCsv.ReadHeader();

            var delimiter = detectCsv.Context.Parser?.Delimiter ?? ",";

            // Second pass: read all rows as strings
            using var reader = new StringReader(text);

            var config = new CsvConfiguration(CultureInfo.InvariantCulture)
            {
                Delimiter = delimiter,
                HasHeaderRecord = true
            };

            using var csv = new CsvReader(reader, config);

            csv.Read();
            csv.ReadHeader();

            var header = csv.HeaderRecord;

            if (header is null || header.Length < 2)
                return [];

            var rows = new List<string[]>();

            while (csv.Read())
            {
                rows.Add(
                    Enumerable
                        .Range(0, header.Length)
                        .Select(i => csv.GetField(i) ?? string.Empty)
                        .ToArray()
                );
            }

            if (rows.Count == 0)
                return [];

            // Guess culture from data rows
            var culture = GuessCulture(rows);

            // Parse dates once
            var parsedRows = rows
                .Select(r => new
                {
                    Date = ParseDate(r[0]),
                    Values = r.Skip(1).ToArray()
                })
                .ToList();

            var result = Enumerable
                .Range(1, header.Length - 1)
                .Select(columnIndex =>
                {
                    var data = parsedRows
                        .Where(r =>
                            r.Date.HasValue &&
                            columnIndex - 1 < r.Values.Length &&
                            !string.IsNullOrWhiteSpace(r.Values[columnIndex - 1]))
                        .Select(r => (
                            r.Date!.Value,
                            double.Parse(
                                r.Values[columnIndex - 1],
                                NumberStyles.Float | NumberStyles.AllowThousands,
                                culture)
                        ));

                    return new TimeSeriesInfo(
                        label: header[columnIndex],
                        data: data
                    );
                })
                .ToArray();

            return result;
        }
    }

    extension(byte[]? bytes)
    {
        public TimeSeriesInfo[] ToTimeSeriesInfo() => bytes?.Length > 0 ? Encoding.UTF8.GetString(bytes).ToTimeSeriesInfo() : [];
    }
}