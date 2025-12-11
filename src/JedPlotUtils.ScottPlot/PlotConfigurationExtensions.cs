using ScottPlot;
using ScottPlot.TickGenerators;

namespace JedPlotUtils.ScottPlot;

public static class PlotConfigurationExtensions
{
    public static void ConfigurePlot(this Plot plot, PlotConfiguration plotConfiguration)
    {
        ConfigureLegend(plot, plotConfiguration.LegendLayout);
        ConfigureXAxis(plot, plotConfiguration.PlotRender.UseDateTimeAxis);
        ConfigurePalette(plot, plotConfiguration.Palette);
    }

    private static void ConfigureXAxis(Plot plot, bool useDateTimeAxis)
    {
        if (useDateTimeAxis)
            plot.Axes.DateTimeTicksBottom();
        else
            plot.Axes.Bottom.TickGenerator = new NumericAutomatic() { IntegerTicksOnly = true };
    }

    private static void ConfigurePalette(Plot plot, IPalette? palette)
    {
        if (palette is not null)
            plot.Add.Palette = palette;
    }

    private static void ConfigureLegend(Plot plot, LegendLayout? legendLayout)
    {
        if (legendLayout is not null)
        {
            var layout = legendLayout.Value;
            var legend = plot.Legend;

            legend.Orientation = layout.Orientation;
            legend.BackgroundColor = layout.BackgroundColor ?? Colors.Transparent;
            legend.BackgroundHatchColor = layout.BackgroundHatchColor ?? Colors.Transparent;
            legend.ShadowColor = layout.ShadowColor ?? Colors.Transparent;
            legend.OutlineColor = layout.OutlineColor ?? Colors.Transparent;
            legend.FontColor = layout.FontColor ?? Colors.Transparent;
            legend.OutlineStyle = layout.OutlineStyle ?? LineStyle.None;

            plot.ShowLegend(layout.Edge);
        }
        else
        {
            plot.HideLegend();
        }
    }
}
