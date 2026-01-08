using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;
using Avalonia.Data;
using Avalonia.Data.Converters;
using JedPlotUtils.ScottPlot.Common.Components;

namespace JedPlotUtils.ScottPlot.Avalonia.Components.Converters;

public class DisplayModeVisibilityConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is DisplayMode displayMode && parameter is string component)
        {
            return component switch
            {
                "Chart" => GetChartVisibility(displayMode),
                "Grid" => GetGridVisibility(displayMode),
                "Splitter" => GetSplitterVisibility(displayMode),
                _ => false
            };
        }

        return BindingOperations.DoNothing;
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => BindingOperations.DoNothing;

    private static bool GetChartVisibility(DisplayMode displayMode) =>
        displayMode switch
        {
            DisplayMode.Grid => false,
            DisplayMode.Chart => true,
            DisplayMode.BothHorizontal => true,
            DisplayMode.BothVertical => true,
            _ => false
        };

    private static bool GetGridVisibility(DisplayMode displayMode) =>
        displayMode switch
        {
            DisplayMode.Grid => true,
            DisplayMode.Chart => false,
            DisplayMode.BothHorizontal => true,
            DisplayMode.BothVertical => true,
            _ => false
        };

    private static bool GetSplitterVisibility(DisplayMode displayMode) =>
        displayMode switch
        {
            DisplayMode.Grid => false,
            DisplayMode.Chart => false,
            DisplayMode.BothHorizontal => true,
            DisplayMode.BothVertical => true,
            _ => false
        };
}
