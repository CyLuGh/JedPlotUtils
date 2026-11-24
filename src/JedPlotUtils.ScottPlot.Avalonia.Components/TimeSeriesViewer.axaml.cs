using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JedPlotUtils.ScottPlot.Avalonia.Components.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.ScottPlot.Avalonia.Components;

public partial class TimeSeriesViewer : ReactiveUserControl<TimeSeriesViewerViewModel>
{
    public TimeSeriesViewer()
    {
        InitializeComponent();
    }
}
