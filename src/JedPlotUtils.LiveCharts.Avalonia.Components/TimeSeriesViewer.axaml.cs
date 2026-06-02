using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using LanguageExt;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using ReactiveUI;
using ReactiveUI.Avalonia;
using AV = Avalonia;
using RxUnit = System.Reactive.Unit;

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
                .Do(vm => vm.DisplayMode = DisplayMode.BothHorizontal)
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
        /* Display Mode */
        viewModel
            .AdaptDisplayModeInteraction.RegisterHandler(ctx =>
            {
                SetGridDimensions(view.GridMainDisplay, ctx.Input);
                SetHierarchyGridConstraints(
                    view.HierarchyGrid,
                    ctx.Input,
                    viewModel.HierarchyGridViewModel
                );
                SetGridSplitterConstraints(view.Splitter, ctx.Input);
                ctx.SetOutput(System.Reactive.Unit.Default);
            })
            .DisposeWith(disposables);

        /* Mouse over chart */
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
                    var date = point.Coordinate.SecondaryValue.ToDateOnly();
                    if (point.Context.Series.Tag is Identifier identifier)
                    {
                        viewModel.HoveredPoint = (identifier, date);
                    }
                    else
                    {
                        viewModel.HoveredPoint = Option<(Identifier, DateOnly)>.None;
                    }
                }
                else
                {
                    viewModel.HoveredPoint = Option<(Identifier, DateOnly)>.None;
                }
            })
            .DisposeWith(disposables);

        viewModel.HighlightChartPointInteraction.RegisterHandler(ctx =>
        {
            ctx.SetOutput(System.Reactive.Unit.Default);
        });
    }

    private static void SetGridSplitterConstraints(GridSplitter splitter, DisplayMode displayMode)
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, 1);

                splitter.VerticalAlignment = AV.Layout.VerticalAlignment.Center;
                splitter.HorizontalAlignment = AV.Layout.HorizontalAlignment.Stretch;

                splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                splitter.ResizeDirection = GridResizeDirection.Rows;

                break;

            case DisplayMode.BothHorizontal:
                Grid.SetColumn(splitter, 1);
                Grid.SetRow(splitter, 0);

                splitter.VerticalAlignment = AV.Layout.VerticalAlignment.Stretch;
                splitter.HorizontalAlignment = AV.Layout.HorizontalAlignment.Center;

                splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                break;

            default:
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, 0);
                break;
        }
    }

    private static void SetHierarchyGridConstraints(
        HierarchyGrid.Avalonia.Grid hierarchyGrid,
        DisplayMode displayMode,
        HierarchyGrid.Definitions.HierarchyGridViewModel hierarchyGridViewModel
    )
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                Grid.SetColumn(hierarchyGrid, 0);
                Grid.SetRow(hierarchyGrid, 2);
                hierarchyGridViewModel.IsTransposed = false;
                break;

            case DisplayMode.BothHorizontal:
                Grid.SetColumn(hierarchyGrid, 2);
                Grid.SetRow(hierarchyGrid, 0);
                hierarchyGridViewModel.IsTransposed = true;
                break;

            default:
                Grid.SetColumn(hierarchyGrid, 0);
                Grid.SetRow(hierarchyGrid, 0);
                hierarchyGridViewModel.IsTransposed = false;
                break;
        }
    }

    private static void SetGridDimensions(Grid grid, DisplayMode displayMode)
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*");
                grid.RowDefinitions = RowDefinitions.Parse("*,Auto,*");
                break;

            case DisplayMode.BothHorizontal:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*,Auto,*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;

            default:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;
        }
    }
}
