using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JedPlotUtils.ViewModels;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public partial class TramoForecastOptionsView : ReactiveUserControl<TramoForecastOptionsViewModel>
{
    public TramoForecastOptionsView()
    {
        InitializeComponent();
    }
}
