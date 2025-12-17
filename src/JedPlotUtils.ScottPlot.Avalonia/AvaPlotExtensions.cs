using Avalonia.Input;
using ScottPlot;
using ScottPlot.Avalonia;

namespace JedPlotUtils.ScottPlot.Avalonia;

public static class AvaPlotExtensions
{
    public static PlotInteractivity SeriesSelection(
        this AvaPlot avaPlot,
        PointerPressedEventArgs evt,
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
        this AvaPlot avaPlot,
        PointerEventArgs evt,
        PlotInteractivity interactivity
    )
    {
        interactivity.Decorations.Hide();
        avaPlot.Refresh();
        evt.Handled = true;
    }

    public static SeriesIndex HandleMouseOver(
        this AvaPlot avaPlot,
        PointerEventArgs evt,
        PlotInteractivity interactivity
    )
    {
        if (interactivity.PlotRender.InteractivityMode == InteractivityMode.None)
            return SeriesIndex.None;

        var plot = avaPlot.Plot;
        var position = evt.GetPosition(avaPlot);
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

        avaPlot.Refresh();
        evt.Handled = true;
        return hoveredInfo;
    }

    public static PlotInteractivity DrawScatterLines(
        this AvaPlot avaPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = avaPlot.Plot.DrawScatterLines(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            avaPlot.UserInputProcessor.UserActionResponses.Clear();

        avaPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, true);
    }

    public static PlotInteractivity DrawScatterPoints(
        this AvaPlot avaPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = avaPlot.Plot.DrawScatterPoints(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            avaPlot.UserInputProcessor.UserActionResponses.Clear();

        avaPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, false);
    }

    public static PlotInteractivity DrawScatterStepLines(
        this AvaPlot avaPlot,
        PlotRender plotRender,
        params IEnumerable<PlotSeries> series
    )
    {
        var (scatters, deco) = avaPlot.Plot.DrawScatterStepLines(series);

        if (plotRender.InteractivityMode != InteractivityMode.None)
            avaPlot.UserInputProcessor.UserActionResponses.Clear();

        avaPlot.Refresh();
        return new PlotInteractivity(scatters, deco, plotRender, PlotSelection.Default, true);
    }
}
