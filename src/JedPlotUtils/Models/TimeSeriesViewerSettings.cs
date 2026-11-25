using JedPlotUtils.Palette;

namespace JedPlotUtils.Models;

public record TimeSeriesViewerSettings
{
    public static readonly string DefaultWebServiceAddress = "http://localhost:4566";
    public static readonly Color DefaultColorAO = Color.Parse("#D62728");
    public static readonly Color DefaultColorLS = Color.Parse("#FF7F0E");
    public static readonly Color DefaultColorTC = Color.Parse("#9467BD");
    public static readonly Color DefaultColorSO = Color.Parse("#E377C2");

    public static TimeSeriesViewerSettings Default =>
        new()
        {
            SeriesSelectionMode = SelectionMode.Single,
            WebServiceAddress = DefaultWebServiceAddress,
            DisplayMode = DisplayMode.BothHorizontal,
            Palette = "Tango",
            DateFormat = "yyyy-MM",
            Decimals = 0,
            HasThousandsSeparators = false,
            ShowStatusBar = true,
            SwitchGridAndChart = false,
            AutoScrollGrid = true,
            AutoConnect = false,
            ColorAO = DefaultColorAO,
            ColorLS = DefaultColorLS,
            ColorTC = DefaultColorTC,
            ColorSO = DefaultColorSO,
            Legend = LegendDisplay.Hidden,
        };

    public required SelectionMode SeriesSelectionMode { get; init; }
    public required string? WebServiceAddress { get; init; }
    public required DisplayMode DisplayMode { get; init; }
    public required string? Palette { get; init; }
    public required string? DateFormat { get; init; }
    public required byte Decimals { get; init; }
    public required bool HasThousandsSeparators { get; init; }
    public required bool ShowStatusBar { get; init; }
    public required bool SwitchGridAndChart { get; init; }
    public required bool AutoConnect { get; init; }
    public required bool AutoScrollGrid { get; init; }
    public required Color ColorAO { get; init; }
    public required Color ColorLS { get; init; }
    public required Color ColorTC { get; init; }
    public required Color ColorSO { get; init; }
    public required LegendDisplay Legend { get; init; }
}
