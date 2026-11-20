using JedPlotUtils.Palette;

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
            Palette = "Tango",
            DateFormat = "yyyy-MM",
            Decimals = 2,
            HasThousandsSeparators = false,
            ShowStatusBar = true,
            SwitchGridAndChart = false,
            AutoScrollGrid = true,
            AutoConnect = false,
        };

    public SelectionMode SeriesSelectionMode { get; init; }
    public string? WebServiceAddress { get; init; }
    public DisplayMode DisplayMode { get; init; }
    public string? Palette { get; init; }
    public string? DateFormat { get; init; }
    public byte Decimals { get; init; }
    public bool HasThousandsSeparators { get; init; }
    public bool ShowStatusBar { get; init; }
    public bool SwitchGridAndChart { get; init; }
    public bool AutoConnect { get; init; }
    public bool AutoScrollGrid { get; init; }
    public Color ColorAO { get; init; }
    public Color ColorLS { get; init; }
    public Color ColorTC { get; init; }
    public Color ColorSO { get; init; }
}
