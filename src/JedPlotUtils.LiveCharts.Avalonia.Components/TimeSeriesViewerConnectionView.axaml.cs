using JedPlotUtils.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public partial class TimeSeriesViewerConnectionView
    : ReactiveUserControl<TimeSeriesViewerConfigurationViewModel>
{
    public TimeSeriesViewerConnectionView()
    {
        InitializeComponent();
    }
}
