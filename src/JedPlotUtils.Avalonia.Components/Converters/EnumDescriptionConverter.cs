using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;

namespace JedPlotUtils.Avalonia.Components.Converters;

public class EnumDescriptionConverter : IValueConverter
{
    public object? Convert(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => value is Enum e ? e.GetDescription() : value?.ToString();

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    ) => BindingOperations.DoNothing;
}
