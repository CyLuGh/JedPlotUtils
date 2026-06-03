using JedPlotUtils.Palette;
using SkiaSharp;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

internal static class ColorExtensions
{
    extension(Color color)
    {
        public SKColor Convert() => new SKColor(color.R, color.G, color.B, color.A);
    }
}
