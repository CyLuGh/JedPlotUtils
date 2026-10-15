using JedPlotUtils.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.Avalonia.Components;

public partial class TimeSeriesViewerConfigurationView
    : ReactiveUserControl<TimeSeriesViewerConfigurationViewModel>
{
    public TimeSeriesViewerConfigurationView()
    {
        InitializeComponent();
    }
}
