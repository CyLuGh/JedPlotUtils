using System;
using System.Diagnostics;
using Avalonia.Media;
using SkiaSharp;
using Topten.RichTextKit;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

public class CustomFontMapper : FontMapper
{
    public SKTypeface DefaultTypeface { get; }

    public CustomFontMapper(SKTypeface defaultTypeface)
    {
        DefaultTypeface = defaultTypeface;
    }

    public override SKTypeface TypefaceFromStyle(IStyle style, bool ignoreFontVariants)
    {
        return DefaultTypeface;
    }
}
