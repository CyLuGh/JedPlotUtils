using System.Collections.Frozen;
using System.Globalization;
using System.Text;
using System.Text.Json;
using System.Xml;
using ClosedXML.Excel;
using CsvHelper;
using CsvHelper.Configuration;
using LanguageExt;

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

        public string ToXmlSpreadSheet()
        {
            var seq = infos.ToSeq();

            using var sw = new StringWriter();
            using var writer = new XmlTextWriter(sw);
            writer.Formatting = Formatting.Indented;

            WriteXmlHeader(writer);
            WriteHeadersRow(writer,seq);
            WriteDataRows(writer,seq);
            WriteXmlFooter(writer);

            return sw.ToString();
        }

        public (string[] headers, object?[][] rows) ToExcelFormat()
        {
            var seq = infos.ToSeq();

            string[] headers = ["Period", .. seq.Map(x => x.Label)];
            var rows = seq
                .GetDates()
                .Items
                .OrderBy(x => x)
                .Select(date => (object?[])
                    [
                        date,
                        ..seq.Map(ts =>
                        ts.Data.Find(
                        date).MatchUnsafe(
                        d => (object)d,
                            () => null))
                    ])
                .ToArray();

            return (headers, rows);
        }

        public byte[] ToExcelBytes()
        {
            using var workbook = new XLWorkbook();
            var worksheet = workbook.Worksheets.Add("Data");

            var (headers, rows) = infos.ToExcelFormat();
            for (int c = 0; c < headers.Length; c++)
                worksheet.Cell(1, c + 1).Value = headers[c];

            worksheet.Cell(2, 1).InsertData(rows);
            worksheet.Column(1).Style.DateFormat.Format = "yyyy-MM-dd";
            worksheet.Columns().AdjustToContents();
            using var stream = new MemoryStream();
            workbook.SaveAs(stream);
            return stream.ToArray();
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

    private static void WriteDataRows(XmlWriter writer, Seq<TimeSeriesInfo> series)
    {
        foreach (var period in series.GetDates().Items.OrderBy(x => x))
        {
            writer.WriteStartElement("Row");
            
            writer.WriteStartElement("Cell");
            writer.WriteStartElement("Data");
            writer.WriteAttributeString("ss:Type", "DateTime");
            writer.WriteString(period.ToString("yyyy-MM-dd")+"T00:00:00.000");
            writer.WriteEndElement(); //data
            writer.WriteEndElement(); //cell

            foreach (var ser in series)
            {
                writer.WriteStartElement("Cell");
                writer.WriteStartElement("Data");
                writer.WriteAttributeString("ss:Type", "Number");
                writer.WriteString(ser.Data.Find(period, d=> d.ToString(CultureInfo.InvariantCulture), () => string.Empty));
                writer.WriteEndElement(); //data
                writer.WriteEndElement(); //cell
            }

            writer.WriteEndElement(); //row
        }
    }

    private static void WriteHeadersRow(XmlWriter writer, Seq<TimeSeriesInfo> series)
    {
        writer.WriteStartElement("Row");

        writer.WriteStartElement("Cell");
        writer.WriteStartElement("Data");
        writer.WriteAttributeString("ss:Type", "String");
        writer.WriteString("Period");
        writer.WriteEndElement(); //data
        writer.WriteEndElement(); //cell

        foreach (var ser in series)
        {
            writer.WriteStartElement("Cell");
            writer.WriteStartElement("Data");
            writer.WriteAttributeString("ss:Type", "String");
            writer.WriteString("Period");
            writer.WriteEndElement(); //data
            writer.WriteEndElement(); //cell
        }

        writer.WriteEndElement(); //row
    }

    private static void WriteXmlHeader(XmlWriter writer)
    {
        writer.WriteStartDocument(true);
        writer.WriteProcessingInstruction("mso-application","progid=\"Excel.Sheet\"");
        writer.WriteStartElement("Workbook");
        writer.WriteAttributeString("xmlns", "urn:schemas-microsoft-com:office:spreadsheet");
        writer.WriteAttributeString("xmlns:o", "urn:schemas-microsoft-com:office:office");
        writer.WriteAttributeString("xmlns:x", "urn:schemas-microsoft-com:office:excel");
        writer.WriteAttributeString("xmlns:ss", "urn:schemas-microsoft-com:office:spreadsheet");
        writer.WriteAttributeString("xmlns:html", "http://www.w3.org/TR/REC-html40");

        writer.WriteStartElement("Worksheet");
        writer.WriteAttributeString("ss:Name", "Sheet1");
        writer.WriteStartElement("Table");
    }

    private static void WriteXmlFooter(XmlWriter writer)
    {
        writer.WriteEndElement(); //table
        writer.WriteEndElement(); //worksheet
        writer.WriteEndElement(); //workbook
        writer.Flush();
    }

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