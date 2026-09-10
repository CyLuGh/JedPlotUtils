using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Text.RegularExpressions;
using CsvHelper;
using CsvHelper.Configuration;
using CsvHelper.Delegates;
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
    [JsonConverter(typeof(IdentifierConverter))]
    public Identifier Identifier { get; }
    public string Label { get; init; }

    [JsonConverter(typeof(DateOnlyDoubleHashMapConverter))]
    public HashMap<DateOnly, double> Data { get; }
    public SeriesChartType ChartType { get; }

    [JsonConverter(typeof(DateOnlyDoubleHashMapConverter))]
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
        ArgumentException.ThrowIfNullOrWhiteSpace(identifier);
        ArgumentNullException.ThrowIfNull(label);

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
        )
    { }

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
        )
    { }

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
        )
    { }

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
        )
    { }

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
        )
    { }

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
        )
    { }

    public TimeSeriesInfo(DateOnly[] dates, double[] values)
        : this(Option<string>.None, Option<string>.None, dates, values) { }

    [JsonConstructor]
    public TimeSeriesInfo(
        Identifier identifier,
        string label,
        HashMap<DateOnly, double> data,
        SeriesChartType chartType,
        HashMap<DateOnly, double> auxiliaryData,
        int index
    )
    {
        Identifier = identifier;
        Label = label;
        Data = data;
        ChartType = chartType;
        AuxiliaryData = auxiliaryData;
        Index = index;
    }
}

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

public sealed class DateOnlyDoubleHashMapConverter : JsonConverter<HashMap<DateOnly, double>>
{
    private const string DateFormat = "yyyy-MM-dd";

    public override HashMap<DateOnly, double> Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        if (reader.TokenType != JsonTokenType.StartObject)
        {
            throw new JsonException("Expected start of object.");
        }

        var result = new HashMap<DateOnly, double>();

        while (reader.Read())
        {
            if (reader.TokenType == JsonTokenType.EndObject)
            {
                return result;
            }

            if (reader.TokenType != JsonTokenType.PropertyName)
            {
                throw new JsonException("Expected property name.");
            }

            var dateString = reader.GetString();

            if (!DateOnly.TryParseExact(dateString, DateFormat, out var date))
            {
                throw new JsonException(
                    $"Invalid date key '{dateString}'. Expected format '{DateFormat}'."
                );
            }

            reader.Read();

            var value = reader.GetDouble();

            result = result.Add(date, value);
        }

        throw new JsonException("Unexpected end of JSON.");
    }

    public override void Write(
        Utf8JsonWriter writer,
        HashMap<DateOnly, double> value,
        JsonSerializerOptions options
    )
    {
        writer.WriteStartObject();

        foreach (var (date, number) in value)
        {
            writer.WriteNumber(date.ToString(DateFormat), number);
        }

        writer.WriteEndObject();
    }
}

public sealed class IdentifierConverter : JsonConverter<Identifier>
{
    public override Identifier Read(
        ref Utf8JsonReader reader,
        Type typeToConvert,
        JsonSerializerOptions options
    )
    {
        using var doc = JsonDocument.ParseValue(ref reader);

        var root = doc.RootElement;

        var id = root.GetProperty("id").GetString()!;
        var guid = root.GetProperty("guid").GetGuid();

        var level = root.TryGetProperty("level", out var levelProp)
            ? Enum.Parse<Level>(levelProp.GetString()!)
            : Level.Primary;

        return new Identifier(id, guid, level);
    }

    public override void Write(
        Utf8JsonWriter writer,
        Identifier value,
        JsonSerializerOptions options
    )
    {
        var (id, guid, level) = value;

        writer.WriteStartObject();

        writer.WriteString("id", id);
        writer.WriteString("guid", guid);

        writer.WriteString("level", level.ToString());

        writer.WriteEndObject();
    }
}
