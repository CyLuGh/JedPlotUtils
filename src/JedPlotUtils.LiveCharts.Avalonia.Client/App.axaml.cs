using System;
using System.IO;
using Avalonia;
using Avalonia.Controls.ApplicationLifetimes;
using Avalonia.Markup.Xaml;
using Avalonia.Platform;
using SkiaSharp;
using Topten.RichTextKit;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

public partial class App : Application
{
    public override void Initialize()
    {
        AvaloniaXamlLoader.Load(this);

        FontMapper.Default = new CustomFontMapper(LoadGridFont());
        LiveChartsCore.LiveCharts.Configure(c =>
            c.AddLiveChartsAppSettings().AddLiveChartsRenderSettings(LoadChartFont())
        );
    }

    public override void OnFrameworkInitializationCompleted()
    {
        if (ApplicationLifetime is IClassicDesktopStyleApplicationLifetime desktop)
        {
            desktop.MainWindow = new MainWindow();
        }

        base.OnFrameworkInitializationCompleted();
    }

    private static SKTypeface LoadGridFont() =>
        LoadFont(
            "avares://JedPlotUtils.LiveCharts.Avalonia.Client/Assets/Fonts/Lexend-VariableFont_wght.ttf"
        );

    private static SKTypeface LoadChartFont() =>
        LoadFont(
            "avares://JedPlotUtils.LiveCharts.Avalonia.Client/Assets/Fonts/LexendZetta-VariableFont_wght.ttf"
        );

    private static SKTypeface LoadFont(string path)
    {
        var uri = new Uri(path);
        using var stream = AssetLoader.Open(uri);
        using var ms = new MemoryStream();
        stream.CopyTo(ms);
        var data = SKData.CreateCopy(ms.ToArray());
        return SKTypeface.FromData(data);
    }
}
