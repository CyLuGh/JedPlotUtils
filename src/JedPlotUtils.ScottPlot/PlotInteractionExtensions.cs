using System.Text;
using LanguageExt;
using ScottPlot;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot;

public static class PlotInteractionExtensions
{
    public static void AllSeriesMouseOver(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation,
        Seq<Scatter> series
    )
    {
        var points = series
            .Select(s => (Scatter: s, Point: s.Data.GetNearestX(mouseLocation, plot.LastRender)))
            .Where(t => !double.IsNaN(t.Point.Y));

        if (points.IsEmpty)
        {
            plotInteractivity.Decorations.Hide();
            return;
        }

        ShowCrosshair(
            plotInteractivity.Decorations.Crosshair,
            points.Head.Point.Coordinates,
            showHorizontalLine: false
        );

        var text = new StringBuilder()
            .AppendLine(plotInteractivity.PlotRender.XFormatter(points.Head.Point.X))
            .AppendJoin(
                Environment.NewLine,
                points.Select(t =>
                    $"{t.Scatter.LegendText}: {plotInteractivity.PlotRender.YFormatter(t.Point.Y)}"
                )
            )
            .ToString();

        plot.ShowText(
            plotInteractivity,
            mouseLocation,
            mouseLocation,
            text,
            backgroundColor: points.Map(x => x.Scatter.Color).MixColors()
        );

        // TODO in imp: call refresh
    }

    public static SeriesIndex SingleSeriesMouseOver(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation,
        Seq<Scatter> series
    )
    {
        var hovered = GetHoveredSeriesIndex(plot, mouseLocation, series);

        // Hide the crosshair, marker and text when no point is found
        if (hovered.Index == -1 || hovered.NearestPoint.IsNone)
        {
            plotInteractivity.Decorations.Hide();
            return SeriesIndex.None;
        }

        // place the crosshair, marker and text over the selected point
        var scatter = series[hovered.Index];
        DataPoint point = hovered.NearestPoint.Match(p => p, () => default);

        plotInteractivity.Decorations.HighlightMarker.HighlightPoint(
            point.Coordinates,
            fillColor: scatter.MarkerFillColor
        );

        var text =
            $"{scatter.LegendText} - {plotInteractivity.PlotRender.XFormatter(point.X)}: {plotInteractivity.PlotRender.XFormatter(point.Y)}";

        plot.ShowText(
            plotInteractivity,
            mouseLocation,
            mouseLocation,
            text,
            backgroundColor: scatter.MarkerFillColor
        );

        return hovered;
    }

    public static void ShowText(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation,
        Coordinates pointLocation,
        string labelText,
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        float offset = 8,
        double opacity = .8
    )
    {
        var (midX, midY) = plot.FindPlotCenter();

        var text = plotInteractivity.Decorations.HighlightText;
        text.AlignHighlightText(mouseLocation, midX, midY, offset);
        text.Location = pointLocation;

        var (fc, bc) = GetColors(foregroundColor, backgroundColor, opacity);
        text.LabelFontColor = fc;
        text.LabelBackgroundColor = bc;
        text.LabelPadding = 5;
        text.LabelText = labelText;
        text.IsVisible = true;
    }

    public static void HighlightPoint(
        this Marker marker,
        Coordinates coordinates,
        Color? outlineColor = null,
        Color? fillColor = null
    )
    {
        var (o, f) = GetColors(outlineColor, fillColor, 1);
        marker.MarkerStyle.OutlineColor = outlineColor ?? o;
        marker.MarkerStyle.FillColor = fillColor ?? f;
        marker.Location = coordinates;
        marker.IsVisible = true;
    }

    public static void ShowCrosshair(
        Crosshair crosshair,
        Coordinates coordinates,
        Color? color = null,
        bool showHorizontalLine = true,
        bool showVerticalLine = true
    )
    {
        if (!showHorizontalLine && !showVerticalLine)
        {
            crosshair.IsVisible = false;
            return;
        }

        crosshair.HorizontalLine.IsVisible = showHorizontalLine;
        crosshair.VerticalLine.IsVisible = showVerticalLine;
        crosshair.Position = coordinates;
        crosshair.HorizontalLine.Color = color ?? Colors.Gray;
        crosshair.VerticalLine.Color = color ?? Colors.Gray;
        crosshair.IsVisible = true;
    }

    private static (double X, double Y) FindPlotCenter(this Plot plot) =>
        (
            plot.Axes.Bottom.Min + ((plot.Axes.Bottom.Max - plot.Axes.Bottom.Min) / 2),
            plot.Axes.Left.Min + ((plot.Axes.Left.Max - plot.Axes.Left.Min) / 2)
        );

    private static void AlignHighlightText(
        this Text highlightText,
        Coordinates mouseLocation,
        double midX,
        double midY,
        float offset
    )
    {
        if (mouseLocation.Y <= midY)
        {
            highlightText.Alignment = Alignment.LowerLeft;
            highlightText.OffsetY = -offset;

            if (mouseLocation.X <= midX)
            {
                highlightText.OffsetX = offset;
            }
            else
            {
                highlightText.OffsetX = -offset;
                highlightText.LabelAlignment = Alignment.LowerRight;
            }
        }
        else
        {
            highlightText.LabelAlignment = Alignment.UpperLeft;
            highlightText.OffsetX = offset;
            highlightText.OffsetY = offset;

            if (mouseLocation.X <= midX)
            {
                highlightText.OffsetX = offset;
            }
            else
            {
                highlightText.OffsetX = -offset;
                highlightText.LabelAlignment = Alignment.UpperRight;
            }
        }
    }

    private static (Color Foreground, Color Background) GetColors(
        Color? foregroundColor = null,
        Color? backgroundColor = null,
        double opacity = .8d
    )
    {
        if (backgroundColor is null)
            return foregroundColor is null
                ? (Colors.Gray, Colors.Transparent)
                : (foregroundColor.Value, Colors.Transparent);

        return foregroundColor is null
            ? (
                backgroundColor.Value.GetMatchingForegroundColor(),
                backgroundColor.Value.WithOpacity(opacity)
            )
            : (foregroundColor.Value, backgroundColor.Value.WithOpacity(opacity));
    }

    private static SeriesIndex GetHoveredSeriesIndex(
        Plot plot,
        Coordinates mouseLocation,
        Seq<Scatter> series
    )
    {
        // get the nearest point of each scatter
        var nearestPoints = series
            .Select((s, i) => (i, s.Data.GetNearest(mouseLocation, plot.LastRender)))
            .ToHashMap();

        // determine which scatter's nearest point is nearest to the mouse
        int scatterIndex = -1;
        double smallestDistance = double.MaxValue;
        for (int i = 0; i < nearestPoints.Count; i++)
        {
            if (nearestPoints[i].IsReal)
            {
                // calculate the distance of the point to the mouse
                double distance = nearestPoints[i].Coordinates.Distance(mouseLocation);
                if (distance < smallestDistance)
                {
                    // store the index
                    scatterIndex = i;
                    smallestDistance = distance;
                }
            }
        }

        return new(scatterIndex, nearestPoints.Find(scatterIndex));
    }

    public static (Seq<Scatter>, PlotDecorations) DrawScatterLines(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        List<Scatter> scatters = [];

        foreach (var cs in series)
        {
            var scatterLine = plot.Add.ScatterLine(cs.Points.ToArray());
            scatterLine.LegendText = cs.Name;
            scatterLine.MarkerStyle.Shape = MarkerShape.None;
            scatterLine.MarkerStyle.Size = 10;
            scatterLine.LineWidth = 3;
            scatters.Add(scatterLine);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (scatters.ToSeq().Strict(), deco);
    }

    public static (Seq<Scatter>, PlotDecorations) DrawScatterPoints(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        List<Scatter> scatters = [];

        foreach (var cs in series)
        {
            var scatterPoints = plot.Add.ScatterPoints(cs.Points.ToArray());
            scatterPoints.LegendText = cs.Name;
            scatterPoints.MarkerStyle.Shape = MarkerShape.FilledCircle;
            scatterPoints.MarkerStyle.Size = 10;
            scatters.Add(scatterPoints);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (scatters.ToSeq().Strict(), deco);
    }

    public static (Seq<Scatter>, PlotDecorations) DrawScatterStepLines(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        List<Scatter> scatters = [];

        foreach (var cs in series)
        {
            var scatterLine = plot.Add.ScatterLine(cs.Points.ToArray());
            scatterLine.LegendText = cs.Name;
            scatterLine.MarkerStyle.Shape = MarkerShape.None;
            scatterLine.MarkerStyle.Size = 8;
            scatterLine.LineWidth = 3;
            scatterLine.ConnectStyle = ConnectStyle.StepHorizontal;
            scatters.Add(scatterLine);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (scatters.ToSeq().Strict(), deco);
    }
}
