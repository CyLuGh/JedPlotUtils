using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using JedPlotUtils.Palette;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.Converters;

public class ColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        return value switch
        {
            IPalette p
                => new DrawMarginFrame()
                {
                    Fill = new SolidColorPaint(p.BackgroundColor.Convert())
                },
            _ => BindingOperations.DoNothing
        };
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => BindingOperations.DoNothing;
}
