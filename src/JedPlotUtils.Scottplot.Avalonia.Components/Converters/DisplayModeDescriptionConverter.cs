using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using JedPlotUtils.ScottPlot.Common.Components;

namespace JedPlotUtils.ScottPlot.Avalonia.Components.Converters;

public class DisplayModeDescriptionConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) =>
        value switch
        {
            DisplayMode displayMode
                => displayMode switch
                {
                    DisplayMode.Grid => "Grid only",
                    DisplayMode.Chart => "Chart only",
                    DisplayMode.BothHorizontal => "Horizontal grid and chart",
                    DisplayMode.BothVertical => "Vertical grid and chart",
                    _ => string.Empty
                },
            _ => BindingOperations.DoNothing
        };

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => BindingOperations.DoNothing;
}
