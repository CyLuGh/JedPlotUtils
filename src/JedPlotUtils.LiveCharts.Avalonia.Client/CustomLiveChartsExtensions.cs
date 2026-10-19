using System.Diagnostics;
using LiveChartsCore.Kernel;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using SkiaSharp;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

/* from https://livecharts.dev/docs/avalonia/2.0.4/overview.installation */

public static partial class CustomLiveChartsExtensions
{
    public static LiveChartsSettings AddLiveChartsAppSettings(this LiveChartsSettings settings) =>
        settings
            // use skia sharp to draw
            .AddSkiaSharp()
            // register mappers for primitives and LiveCharts objects
            .AddDefaultMappers()
            // adds a theme that is light or dark depending on the OS theme;
            // OS theme awareness is not supported in WPF, WinForms and ETO.
            // themes registered here are handled by livecharts and can be used
            // across all the supported UI frameworks, but you could also
            // use XAML based themes if your platform supports them.
            .AddDefaultTheme(
            // if neccessary, override the default theme, for more info see:
            // https://github.com/beto-rodriguez/LiveCharts2/blob/master/samples/ViewModelsSamples/LiveChartsThemeExtensions.cs

            //theme =>
            //    theme.OnInitialized(() =>
            //    {
            //        theme.AnimationsSpeed = TimeSpan.FromSeconds(1);
            //        theme.EasingFunction = EasingFunctions.BounceOut;
            //        theme.Colors = theme.IsDark
            //            ? ColorPalletes.MaterialDesign200
            //            : ColorPalletes.MaterialDesign800;
            //    })
            )
            // loads the recommended settings, this is equivalent to calling
            // .AddSkiaSharp().AddDefaultMappers().AddDefaultTheme()
            .UseDefaults()
    // register your own mappers, this is useful to use custom objects
    // in your plots, for example the next line registers a mapper for the
    // City record, this mapper will be used to convert the City to a coordinate.
    // you can learn more about mappers at:
    // https://livecharts.dev/docs/avalonia/2.0.4/Overview.Mappers
    // .HasMap<City>(
    //     // the maper takes the City and its index in the collection,
    //     // and returns a coordinate, in this case we are using the
    //     // City.Population as Y and the index as the X.
    //     (city, index) => new(index, city.Population)
    // )
    // RTL (right-to-left) shaping is handled already in the library by Harfbuzz,
    // but we can enable RTL to force RTL tooltips and legends.
    // RTL settings are enabled when the system culture is RTL,
    // but you can also force the use of RTL settings by calling:
    //.UseRightToLeftSettings()
    ;

    public static LiveChartsSettings AddLiveChartsRenderSettings(
        this LiveChartsSettings settings,
        SKTypeface? defaultTypeface = null
    ) =>
        settings
            // Configure the global rendering settings
            // this is the default configuration used by LiveCharts,
            // uncomment the lines below to override the defaults.
            // this has no effect in Avalonia or Uno with skia rendering.
            .HasRenderingSettings(settings =>
            {
                // enabling GPU may improve performance but could not be as stable as CPU rendering.
                // settings.UseGPU = false;

                // when Vsync is enabled the charts will render in sync with the screen refresh rate,
                // this improves animations quality and lets the system handle the refresh rate.
                settings.TryUseVSync = true;

                // if Vsync is disabled, then the charts will try to render at the specified FPS.
                // settings.LiveChartsRenderLoopFPS = 60;

                // shows the current FPS in the top-left corner of the chart,
                // settings.ShowFPS = false;
            })
            // LiveCharts uses some defaults for text rendering, but you can
            // improve the configuration to fit your app needs.
            .HasTextSettings(
                new TextSettings
                {
                    // A typeface to use as a default when a paint does not
                    // explicitly set a typeface, this could improve the
                    // text quality but requires the font to be installed.
                    // example: SKTypeface.FromFamilyName("Arial")
                    DefaultTypeface = defaultTypeface ?? SKTypeface.Default,

                    // This function is called every time text is drawn,
                    // it allows you to configure the paint and font before
                    // drawing, you could improve the text quality by changing
                    // Edging, Hinting, etc.
                    FontBuilder = (paint, typeface, size) =>
                    {
                        // This is the default used by LiveCharts:
                        // you can improve this function to suit your needs.

                        // paint.TextSize = size;
                        // paint.Typeface = typeface;
                        // paint.IsAntialias = true;
                        // paint.LcdRenderText = true;


                        return new SKFont(typeface, size)
                        {
                            Edging = SKFontEdging.Antialias,
                            Hinting = SKFontHinting.None,
                            Subpixel = true,
                            LinearMetrics = true
                        };
                    }
                }
            )
    // advanced settings, register your own rendering factory, for example:
    // https://github.com/beto-rodriguez/LiveCharts2/blob/master/src/skiasharp/LiveChartsCore.SkiaSharpView.Maui/MotionCanvas.cs#L43
    // in that link we use the CPU or GPU render mode depending on the user settings, the GPU uses
    // the SkiaSharp hardware accelerated views.
    //.HasRenderingFactory(
    //    (settings, forceGPU) =>
    //    {
    //        // return your renderer here.
    //    })
    ;
}
