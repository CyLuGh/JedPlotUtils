using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using JedPlotUtils.Models;
using LiveChartsCore.Measure;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.Converters;

public class LegendConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is LegendDisplay legendDisplay && targetType == typeof(LegendPosition))
        {
            return legendDisplay switch
            {
                LegendDisplay.Hidden => LegendPosition.Hidden,
                LegendDisplay.Bottom => LegendPosition.Bottom,
                LegendDisplay.Top => LegendPosition.Top,
                LegendDisplay.Left => LegendPosition.Left,
                LegendDisplay.Right => LegendPosition.Right,
                _ => throw new ArgumentOutOfRangeException(),
            };
        }

        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        return BindingOperations.DoNothing;
    }
}
