using Avalonia.Controls;
using JedPlotUtils.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public partial class RenameSeriesView : ReactiveUserControl<RenameSeriesViewModel>
{
    public RenameSeriesView()
    {
        InitializeComponent();
    }
}
