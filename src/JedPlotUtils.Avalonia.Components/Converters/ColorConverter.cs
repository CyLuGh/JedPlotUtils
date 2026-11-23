using System.Globalization;
using Avalonia.Data;
using Avalonia.Data.Converters;
using Avalonia.Media;
using Color = JedPlotUtils.Palette.Color;

namespace JedPlotUtils.Avalonia.Components.Converters;

public class ColorConverter : IValueConverter
{
    public object? Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        switch (value)
        {
            case Color color when targetType == typeof(HsvColor):
            {
                var hsv = color.ToHsv();
                return HsvColor.FromAhsv(hsv.A, hsv.H, hsv.S, hsv.V);
            }
            case HsvColor hsvColor when targetType == typeof(Color):
            {
                var rgb = hsvColor.ToRgb();
                return new Color(rgb.A, rgb.R, rgb.G, rgb.B);
            }
            default:
                return BindingOperations.DoNothing;
        }
    }

    public object? ConvertBack(
        object? value,
        Type targetType,
        object? parameter,
        CultureInfo culture
    )
    {
        switch (value)
        {
            case Color color when targetType == typeof(HsvColor):
            {
                var hsv = color.ToHsv();
                return HsvColor.FromAhsv(hsv.A, hsv.H, hsv.S, hsv.V);
            }
            case HsvColor hsvColor when targetType == typeof(Color):
            {
                var rgb = hsvColor.ToRgb();
                return new Color(rgb.A, rgb.R, rgb.G, rgb.B);
            }
            default:
                return BindingOperations.DoNothing;
        }
    }
}
