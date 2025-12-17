using System.Windows.Input;
using ScottPlot;
using ScottPlot.WPF;

namespace JedPlotUtils.ScottPlot.WPF;

public static class WpfPlotExtensions
{
    public static PlotInteractivity SeriesSelection(
        this WpfPlot avaPlot,
        MouseEventArgs evt,
        PlotInteractivity interactivity
    )
    {
        var plot = avaPlot.Plot;
        var position = evt.GetPosition(avaPlot);
        Pixel mousePixel = new(position.X, position.Y);
        Coordinates mouseLocation = plot.GetCoordinates(mousePixel);

        return plot.SeriesSelection(interactivity, mouseLocation);
    }

    public static void HandleMouseLeft(
        this WpfPlot wpfPlot,
        MouseEventArgs evt,
        PlotInteractivity interactivity
    )
    {
        interactivity.Decorations.Hide();
        wpfPlot.Refresh();
        evt.Handled = true;
    }

    public static SeriesIndex HandleMouseOver(
        this WpfPlot wpfPlot,
        MouseEventArgs evt,
        PlotInteractivity interactivity
    )
    {
        if (interactivity.PlotRender.InteractivityMode == InteractivityMode.None)
            return SeriesIndex.None;

        var plot = wpfPlot.Plot;
        var position = evt.GetPosition(wpfPlot);
        Pixel mousePixel = new(position.X, position.Y);
        Coordinates mouseLocation = plot.GetCoordinates(mousePixel);

        var hoveredInfo = SeriesIndex.None;

        switch (interactivity.PlotRender.InteractivityMode)
        {
            case InteractivityMode.SingleSeries:
                hoveredInfo = plot.SingleSeriesMouseOver(interactivity, mouseLocation);
                break;
            case InteractivityMode.AllSeries:
                plot.AllSeriesMouseOver(interactivity, mouseLocation);
                break;
        }

        wpfPlot.Refresh();
        evt.Handled = true;
        return hoveredInfo;
    }

    public static PlotInteractivity DrawScatterLines(
        this WpfPlot wpfPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = wpfPlot.Plot.DrawScatterLines(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            wpfPlot.UserInputProcessor.UserActionResponses.Clear();

        wpfPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, true);
    }

    public static PlotInteractivity DrawScatterPoints(
        this WpfPlot wpfPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = wpfPlot.Plot.DrawScatterPoints(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            wpfPlot.UserInputProcessor.UserActionResponses.Clear();

        wpfPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, false);
    }

    public static PlotInteractivity DrawScatterStepLines(
        this WpfPlot wpfPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = wpfPlot.Plot.DrawScatterStepLines(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            wpfPlot.UserInputProcessor.UserActionResponses.Clear();

        wpfPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, true);
    }
}
