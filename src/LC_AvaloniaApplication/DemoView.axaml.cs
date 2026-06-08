using System;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace LC_AvaloniaApplication;

public partial class DemoView : ReactiveUserControl<DemoViewModel>
{
    public IPalette[] Palettes { get; }

    public DemoView()
    {
        ViewModel = new DemoViewModel();

        InitializeComponent();

        Palettes = [new TangoPalette(), new SolarizedPalette()];
        ComboBoxPalettes.ItemsSource = Palettes;

        ComboBoxDisplayModes.ItemsSource = Enum.GetValues<DisplayMode>();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .WhereNotNull()
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private static void PopulateFromViewModel(
        DemoView view,
        DemoViewModel viewModel,
        CompositeDisposable disposables
    )
    {
        //view.TimeSeriesViewer.ViewModel = viewModel.TimeSeriesViewModel;
    }
}
