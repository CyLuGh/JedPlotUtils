using Avalonia.Controls;
using JedPlotUtils.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.Avalonia.Components;

public partial class TimeSeriesViewerConfigurationView
    : ReactiveUserControl<TimeSeriesViewerConfigurationViewModel>
{
    public TimeSeriesViewerConfigurationView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            Signal
                .Return(RxVoid.Default)
                .Subscribe(_ =>
                {
                    var focusManager = TopLevel.GetTopLevel(this)?.FocusManager;
                    focusManager?.Focus(ApplyButton);
                })
                .DisposeWith(disposables);
        });
    }
}
