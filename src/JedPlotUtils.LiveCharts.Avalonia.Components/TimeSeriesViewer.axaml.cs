using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using LanguageExt;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public partial class TimeSeriesViewer : ReactiveUserControl<TimeSeriesViewerViewModel>
{
    public TimeSeriesViewer()
    {
        InitializeComponent();

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
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel,
        CompositeDisposable disposables
    )
    {
        Observable
            .FromEvent<
                ChartPointHoverHandler,
                (
                    IChartView chart,
                    IEnumerable<ChartPoint>? newItems,
                    IEnumerable<ChartPoint>? oldItems
                )
            >(
                handler => (chart, newItems, oldItems) => handler((chart, newItems, oldItems)),
                h => view.CartesianChart.HoveredPointsChanged += h,
                h => view.CartesianChart.HoveredPointsChanged -= h
            )
            .Subscribe(x =>
            {
                var (chart, newItems, oldItems) = x;

                var nItems = newItems?.ToSeq() ?? Seq<ChartPoint>.Empty;

                if (!nItems.IsEmpty)
                {
                    var point = nItems[0];
                    var date = point.Coordinate.PrimaryValue;
                    var identifier = point.Context.Series.Tag as Identifier?;
                }
                else
                {
                    viewModel.HoveredPoint = Option<(Identifier, DateOnly)>.None;
                }
            })
            .DisposeWith(disposables);
    }
}
