namespace JedPlotUtils.Models;

public record TimeSeriesViewerSettings
{
    public static readonly string DefaultWebServiceAddress = "http://localhost:4566";

    public static TimeSeriesViewerSettings Default =>
        new()
        {
            SeriesSelectionMode = SelectionMode.Single,
            WebServiceAddress = DefaultWebServiceAddress,
            DisplayMode = DisplayMode.BothHorizontal,
            Palette = "Tango"
        };

    public SelectionMode SeriesSelectionMode { get; set; }
    public string? WebServiceAddress { get; set; }
    public DisplayMode DisplayMode { get; set; }
    public string? Palette { get; set; }
}
