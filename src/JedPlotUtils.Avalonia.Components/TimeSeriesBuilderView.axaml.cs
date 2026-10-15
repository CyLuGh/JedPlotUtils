using JedPlotUtils.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.Avalonia.Components;

public partial class TimeSeriesBuilderView : ReactiveUserControl<TimeSeriesBuilderViewModel>
{
    public TimeSeriesBuilderView()
    {
        InitializeComponent();
    }
}
