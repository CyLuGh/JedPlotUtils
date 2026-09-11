using System.Globalization;
using ClosedXML.Excel;
using LanguageExt;

namespace JedPlotUtils.Models;

public static class ExcelTimeSeriesInfoParser
{
    public static TimeSeriesInfo[] FromExcel(string filePath)
    {
        using var workbook = new XLWorkbook(filePath);
        var worksheet = workbook.Worksheets.First();
        var range = worksheet.RangeUsed();

        if (range is null)
            return [];

        var rows = range.RowsUsed().ToList();

        if (rows.Count < 2)
            return [];

        var headers = rows[0].Cells().Select(c => c.GetString().Trim()).ToArray();

        if (headers.Length < 2)
            return [];

        var result = new List<TimeSeriesInfo>();

        for (var columnIndex = 2; columnIndex <= headers.Length; columnIndex++)
        {
            var data = rows.Skip(1)
                .Select(row =>
                {
                    var dateCell = row.Cell(1);
                    var valueCell = row.Cell(columnIndex);

                    if (dateCell.IsEmpty() || valueCell.IsEmpty())
                        return Option<(DateOnly, double)>.None;

                    DateOnly date;

                    if (dateCell.TryGetValue<DateTime>(out var dateTime))
                    {
                        date = DateOnly.FromDateTime(dateTime);
                    }
                    else if (
                        !DateOnly.TryParse(
                            dateCell.GetString(),
                            CultureInfo.InvariantCulture,
                            out date
                        )
                    )
                    {
                        return Option<(DateOnly, double)>.None;
                    }

                    if (valueCell.TryGetValue<double>(out var value))
                    {
                        // Numeric Excel cell
                    }
                    else if (
                        !double.TryParse(
                            valueCell.GetString(),
                            NumberStyles.Float | NumberStyles.AllowThousands,
                            CultureInfo.InvariantCulture,
                            out value
                        )
                    )
                    {
                        return Option<(DateOnly, double)>.None;
                    }

                    return (date, value);
                })
                .Somes();

            result.Add(new TimeSeriesInfo(label: headers[columnIndex - 1], data: data));
        }

        return [.. result];
    }
}
