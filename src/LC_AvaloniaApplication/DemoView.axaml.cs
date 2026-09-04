using System;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;

namespace LC_AvaloniaApplication;

public partial class DemoView : ReactiveUserControl<DemoViewModel>
{
    public DemoView()
    {
        ViewModel = new DemoViewModel();

        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm is not null)
                .Select(vm => vm!)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private static void PopulateFromViewModel(
        DemoView view,
        DemoViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        //view.TimeSeriesViewer.ViewModel = viewModel.TimeSeriesViewModel;
    }
}
