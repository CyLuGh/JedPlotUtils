using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using LanguageExt;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Avalonia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using AV = Avalonia;
using RxUnit = System.Reactive.Unit;
using SelectionMode = JedPlotUtils.Models.SelectionMode;

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

        /* Mouse click */
        Observable
            .FromEventPattern<EventHandler<PointerPressedEventArgs>, PointerPressedEventArgs>(
                o =>
                    (o, args) =>
                    {
                        if (viewModel.SelectionMode == SelectionMode.None)
                            return;

                        var chart = (CartesianChart)o!;
                        var pos = args.GetPosition(chart);

                        // Convert Avalonia point → LiveCharts point
                        var lvcPoint = new LvcPoint((float)pos.X, (float)pos.Y);

                        // Hit test
                        var found = chart.CoreChart.FindHoveredPointsBy(lvcPoint).ToSeq();

                        if (found.IsEmpty)
                        {
                            viewModel.Selection = LanguageExt.HashSet<Identifier>.Empty;
                            return;
                        }

                        viewModel.Selection = viewModel.SelectionMode switch
                        {
                            SelectionMode.Single => new([(Identifier)found[0].Context.Series.Tag]),
                            SelectionMode.Multiple => viewModel.Selection.Add(
                                (Identifier)found[0].Context.Series.Tag
                            ),
                            _ => viewModel.Selection,
                        };
                    },
                handler => view.CartesianChart.PointerPressed += handler,
                handler => view.CartesianChart.PointerPressed -= handler
            )
            .Subscribe()
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
                    if (
                        point.Context is
                        { DataSource: DateTimePoint dtp, Series.Tag: Identifier identifier }
                    )
                    {
                        viewModel.HoveredPoint = (identifier, DateOnly.FromDateTime(dtp.DateTime));
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
            ctx.Input.IfSome(t =>
            {
                var (identifier, period) = t;
                var dateTime = period.ToDateTime(TimeOnly.MinValue);

                var chartPoints =
                    from s in view.CartesianChart.Series.Find(s =>
                        s.Tag?.Equals(identifier) == true
                    )
                    from pt in s.Fetch(view.CartesianChart.CoreChart)
                        .Where(p =>
                            p.Context.DataSource is DateTimePoint op && op.DateTime == dateTime
                        )
                    select pt;

                view.CartesianChart.Tooltip?.Show(chartPoints, view.CartesianChart.CoreChart);
            });

            ctx.Input.IfNone(() =>
            {
                view.CartesianChart.Tooltip?.Hide(view.CartesianChart.CoreChart);
            });

            ctx.SetOutput(RxUnit.Default);
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
