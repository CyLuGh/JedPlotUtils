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
        Coordinates mouseLocation
    )
    {
        var map = plotInteractivity.Series;
        var series = map.Keys.ToSeq();
        /*
         * Using only nearest x series may miss some data, that's why the process will do in two steps: first detect any close
         * element then look through all series for items with the same X
         */
        var candidates = series
            .Select(s => s.Data.GetNearestX(mouseLocation, plot.LastRender))
            .Where(t => !double.IsNaN(t.Y));

        if (candidates.IsEmpty)
        {
            plotInteractivity.Decorations.Hide();
            return;
        }

        // Find all series with data for the same X
        var points = series
            .Select(s =>
                from npt in s.Data.GetScatterPoints().Find(c => c.X.Equals(candidates.Head.X))
                select (Scatter: s, Coordinates: npt)
            )
            .Somes()
            .Strict();

        ShowCrosshair(
            plotInteractivity.Decorations.Crosshair,
            points.Head.Coordinates,
            showHorizontalLine: false
        );

        var text = new StringBuilder()
            .AppendLine(plotInteractivity.PlotRender.XFormatter(points.Head.Coordinates.X))
            .AppendJoin(
                Environment.NewLine,
                points.Select(t =>
                    $"{t.Scatter.LegendText}: {plotInteractivity.PlotRender.YFormatter(t.Coordinates.Y)}"
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
    }

    public static SeriesIndex SingleSeriesMouseOver(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation
    )
    {
        var map = plotInteractivity.Series;
        var series = map.Keys.ToSeq();
        var hovered = GetSeriesIndexForLocation(plot, mouseLocation, map);

        // Hide the crosshair, marker and text when no point is found
        if (hovered.IsEmpty)
        {
            plotInteractivity.Decorations.Hide();
            return SeriesIndex.None;
        }

        // place the crosshair, marker and text over the selected point
        var scatter = series[hovered.Index];
        DataPoint point = hovered.NearestPoint.Match(p => p, () => default);

        HighlightPoint(plot, plotInteractivity, mouseLocation, scatter, point);

        return hovered;
    }

    public static void HighlightPoint(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation,
        Scatter scatter,
        DataPoint point
    )
    {
        plotInteractivity.Decorations.HighlightMarker.HighlightPoint(
            point.Coordinates,
            fillColor: scatter.MarkerFillColor
        );

        var text =
            $"{scatter.LegendText} - {plotInteractivity.PlotRender.XFormatter(point.X)}: {plotInteractivity.PlotRender.YFormatter(point.Y)}";

        plot.ShowText(
            plotInteractivity,
            mouseLocation,
            mouseLocation,
            text,
            backgroundColor: scatter.MarkerFillColor
        );
    }

    public static PlotInteractivity SeriesSelection(
        this Plot plot,
        PlotInteractivity plotInteractivity,
        Coordinates mouseLocation
    )
    {
        var map = plotInteractivity.Series;
        var series = map.Keys.ToSeq();
        var currentSelection = plotInteractivity.PlotSelection;

        if (currentSelection.SelectionMode == SelectionMode.None)
            return plotInteractivity;

        var clicked = GetSeriesIndexForLocation(plot, mouseLocation, map);

        if (clicked.IsEmpty)
        {
            currentSelection = currentSelection with { Selection = Seq<Scatter>.Empty };
            foreach (var scatter in series)
            {
                scatter.LineWidth = 3;
                scatter.LinePattern = LinePattern.Solid;
            }
        }
        else
        {
            var added = series[clicked.Index];
            currentSelection =
                currentSelection.SelectionMode == SelectionMode.Single
                    ? currentSelection with
                    {
                        Selection = Seq.create(added)
                    }
                    : currentSelection with
                    {
                        Selection = currentSelection.Selection.Add(added)
                    };

            foreach (var scatter in series)
            {
                scatter.LineWidth = 2;
                scatter.LinePattern = LinePattern.Dotted;
            }

            foreach (var scatter in currentSelection.Selection)
            {
                scatter.LineWidth = 5;
                scatter.LinePattern = LinePattern.Solid;
            }
        }

        return plotInteractivity with
        {
            PlotSelection = currentSelection
        };
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

    private static SeriesIndex GetSeriesIndexForLocation(
        Plot plot,
        Coordinates mouseLocation,
        HashMap<Scatter, string> map
    )
    {
        var series = map.Keys.ToSeq();

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

        return new(
            scatterIndex,
            scatterIndex != -1 ? map[series[scatterIndex]] : Option<string>.None,
            nearestPoints.Find(scatterIndex)
        );
    }

    public static (HashMap<Scatter, string>, PlotDecorations) DrawScatterLines(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        var map = new HashMap<Scatter, string>();

        foreach (var cs in series)
        {
            var scatterLine = plot.Add.ScatterLine(cs.Points.ToArray());
            scatterLine.LegendText = cs.Name;
            scatterLine.MarkerStyle.Shape = MarkerShape.None;
            scatterLine.MarkerStyle.Size = 10;
            scatterLine.LineWidth = 3;

            map = map.Add(scatterLine, cs.Identifier);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (map, deco);
    }

    public static (HashMap<Scatter, string>, PlotDecorations) DrawScatterPoints(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        var map = new HashMap<Scatter, string>();

        foreach (var cs in series)
        {
            var scatterPoints = plot.Add.ScatterPoints(cs.Points.ToArray());
            scatterPoints.LegendText = cs.Name;
            scatterPoints.MarkerStyle.Shape = MarkerShape.FilledCircle;
            scatterPoints.MarkerStyle.Size = 10;
            map = map.Add(scatterPoints, cs.Identifier);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (map, deco);
    }

    public static (HashMap<Scatter, string>, PlotDecorations) DrawScatterStepLines(
        this Plot plot,
        params IEnumerable<PlotSeries> series
    )
    {
        plot.Clear();
        var map = new HashMap<Scatter, string>();

        foreach (var cs in series)
        {
            var scatterLine = plot.Add.ScatterLine(cs.Points.ToArray());
            scatterLine.LegendText = cs.Name;
            scatterLine.MarkerStyle.Shape = MarkerShape.None;
            scatterLine.MarkerStyle.Size = 8;
            scatterLine.LineWidth = 3;
            scatterLine.ConnectStyle = ConnectStyle.StepHorizontal;
            map = map.Add(scatterLine, cs.Identifier);
        }

        var deco = new PlotDecorations(plot);
        plot.Axes.AutoScale();

        return (map, deco);
    }
}
