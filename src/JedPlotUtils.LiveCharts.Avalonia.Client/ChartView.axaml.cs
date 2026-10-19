using Avalonia.Controls;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

public partial class ChartView : UserControl
{
    public ChartView()
    {
        InitializeComponent();
        var tsvvm = new TimeSeriesViewerViewModel();
        ChartViewHost.ViewModel = tsvvm;
    }
}
