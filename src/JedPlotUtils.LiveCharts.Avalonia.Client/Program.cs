using System;
using Avalonia;
using Avalonia.Media;
using Avalonia.Media.Fonts;
using JedPlotUtils.LiveCharts.Avalonia.Components;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

class Program
{
    // Initialization code. Don't use any Avalonia, third-party APIs or any
    // SynchronizationContext-reliant code before AppMain is called: things aren't initialized
    // yet and stuff might break.
    [STAThread]
    public static void Main(string[] args) =>
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);

    // Avalonia configuration, don't remove; also used by visual designer.
    public static AppBuilder BuildAvaloniaApp() =>
        AppBuilder
            .Configure<App>()
            .UsePlatformDetect()
#if DEBUG
            .WithDeveloperTools()
#endif
            .ConfigureFonts(fontManager =>
            {
                fontManager.AddFontCollection(
                    new EmbeddedFontCollection(
                        new("fonts:MyFonts", UriKind.Absolute),
                        new(
                            "avares://JedPlotUtils.LiveCharts.Avalonia.Client/Assets/Fonts",
                            UriKind.Absolute
                        )
                    )
                );
            })
            .With(new FontManagerOptions() { DefaultFamilyName = "fonts:MyFonts#Lexend" })
            .LogToTrace()
            .UseReactiveUI(rx =>
            {
                rx.RegisterView<TimeSeriesViewer, TimeSeriesViewerViewModel>();
            });
}
