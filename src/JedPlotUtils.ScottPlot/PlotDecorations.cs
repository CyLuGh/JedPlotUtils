using ScottPlot;
using ScottPlot.Plottables;

namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotDecorations
{
    public Crosshair Crosshair { get; }
    public Marker HighlightMarker { get; }
    public Text HighlightText { get; }

    public PlotDecorations(Plot plot)
        : this(plot.Add.Crosshair(0, 0), plot.Add.Marker(0, 0), plot.Add.Text("", 0, 0))
    {
        ArgumentNullException.ThrowIfNull(plot);
    }

    public PlotDecorations(Crosshair crosshair, Marker highlightMarker, Text highlightText)
    {
        ArgumentNullException.ThrowIfNull(crosshair);
        ArgumentNullException.ThrowIfNull(highlightMarker);
        ArgumentNullException.ThrowIfNull(highlightText);

        highlightMarker.Shape = MarkerShape.FilledCircle;
        highlightMarker.Size = 9;
        highlightMarker.LineWidth = 2;

        highlightText.LabelAlignment = Alignment.LowerLeft;
        highlightText.LabelBold = true;
        highlightText.OffsetX = 7;
        highlightText.OffsetY = -7;
        highlightText.LabelFontSize = 14f;

        crosshair.IsVisible = false;
        highlightMarker.IsVisible = false;
        highlightText.IsVisible = false;

        Crosshair = crosshair;
        HighlightMarker = highlightMarker;
        HighlightText = highlightText;
    }

    public void Hide()
    {
        Crosshair.IsVisible = false;
        HighlightMarker.IsVisible = false;
        HighlightText.IsVisible = false;
    }
}
