using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using SkiaSharp;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

internal readonly record struct DrawingInfo(
    float Thickness,
    SolidColorPaint Stroke,
    SolidColorPaint? Fill,
    double GeometrySize,
    SolidColorPaint? GeometryStroke,
    SolidColorPaint? GeometryFill
)
{
    public void Deconstruct(
        out float thickness,
        out SolidColorPaint stroke,
        out SolidColorPaint? fill,
        out double geometrySize,
        out SolidColorPaint? geometryStroke,
        out SolidColorPaint? geometryFill
    )
    {
        thickness = Thickness;
        stroke = Stroke;
        fill = Fill;
        geometrySize = GeometrySize;
        geometryStroke = GeometryStroke;
        geometryFill = GeometryFill;
    }
}

internal static class ChartUtils
{
    internal static DrawingInfo GetInfos(
        TimeSeriesInfo tsi,
        LanguageExt.HashSet<Identifier> selection,
        IPalette palette
    )
    {
        var thickness = GetThickness(tsi, selection);
        var stroke = GetStroke(tsi, selection, thickness, palette);
        var fill = GetFill(tsi, selection, palette);
        var geometrySize = GetGeometrySize(tsi);
        var geometryStroke = GetGeometryStroke(tsi, selection, thickness, palette);
        var geometryFill = GetGeometryFill(tsi, selection, palette);
        return new DrawingInfo(thickness, stroke, fill, geometrySize, geometryStroke, geometryFill);
    }

    private static float GetThickness(TimeSeriesInfo tsi, LanguageExt.HashSet<Identifier> selection)
    {
        return selection.IsEmpty
            ? 2
            : selection.Contains(tsi.Identifier)
                ? 3
                : 1;
    }

    private static SolidColorPaint GetStroke(
        TimeSeriesInfo tsi,
        LanguageExt.HashSet<Identifier> selection,
        float thickness,
        IPalette palette
    )
    {
        return selection.IsEmpty || selection.Contains(tsi.Identifier)
            ? new SolidColorPaint(palette.GetColor(tsi.Index).Convert(), thickness)
            : new SolidColorPaint(palette.GetColor(tsi.Index).Convert(), thickness)
            {
                PathEffect = new DashEffect([3, 2])
            };
    }

    private static SolidColorPaint GetGeometryStroke(
        TimeSeriesInfo tsi,
        LanguageExt.HashSet<Identifier> selection,
        float thickness,
        IPalette palette
    )
    {
        return selection.IsEmpty || selection.Contains(tsi.Identifier)
            ? new SolidColorPaint(palette.GetColor(tsi.Index).Convert(), thickness)
            : new SolidColorPaint(SKColors.Transparent, thickness);
    }

    private static SolidColorPaint GetGeometryFill(
        TimeSeriesInfo tsi,
        LanguageExt.HashSet<Identifier> selection,
        IPalette palette
    )
    {
        return new SolidColorPaint(palette.GetColor(tsi.Index).Convert());
    }

    private static SolidColorPaint? GetFill(
        TimeSeriesInfo tsi,
        LanguageExt.HashSet<Identifier> selection,
        IPalette palette
    )
    {
        return tsi.ChartType switch
        {
            SeriesChartType.Area
                => new SolidColorPaint(
                    palette
                        .GetColor(tsi.Index)
                        .Convert()
                        .WithAlpha(
                            selection.IsEmpty || selection.Contains(tsi.Identifier)
                                ? (byte)60
                                : (byte)20
                        )
                ),
            SeriesChartType.Column
                => new SolidColorPaint(
                    palette
                        .GetColor(tsi.Index)
                        .Convert()
                        .WithAlpha(
                            selection.IsEmpty || selection.Contains(tsi.Identifier)
                                ? (byte)60
                                : (byte)20
                        )
                ),
            SeriesChartType.Range
                => new SolidColorPaint(
                    palette
                        .GetColor(tsi.Index)
                        .Convert()
                        .WithAlpha(
                            selection.IsEmpty || selection.Contains(tsi.Identifier)
                                ? (byte)40
                                : (byte)20
                        )
                ),
            _ => null
        };
    }

    private static double GetGeometrySize(TimeSeriesInfo tsi) =>
        tsi.Level switch
        {
            Level.Primary => tsi.ChartType == SeriesChartType.Area ? 4d : 6d,
            Level.Secondary => 3d,
            Level.Tertiary => 0d,
            _ => 12d
        };
}
