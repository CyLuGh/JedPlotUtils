namespace JedPlotUtils.ScottPlot;

public readonly record struct PlotRender
{
    public static readonly PlotRender Default =
        new()
        {
            UseDateTimeAxis = true,
            InteractivityMode = InteractivityMode.AllSeries,
            XFormatter = d => $"{DateTime.FromOADate(d):yyyy-MM-dd}",
            YFormatter = d => $"{d:N}"
        };

    public PlotRender() { }

    public bool UseDateTimeAxis { get; init; } = true;
    public InteractivityMode InteractivityMode { get; init; } = InteractivityMode.AllSeries;
    public Func<double, string> XFormatter { get; init; } =
        d => $"{DateTime.FromOADate(d):yyyy-MM-dd}";
    public Func<double, string> YFormatter { get; init; } = d => $"{d:N}";
}
