using System.Text;
using System.Text.Json;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using DocumentFormat.OpenXml.Office.PowerPoint.Y2021.M06.Main;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using JedPlotUtils.ViewModels;
using LanguageExt;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Avalonia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Extensions;
using ReactiveUI.Primitives.Signals;
using Splat;
using AV = Avalonia;
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
                .Where(vm => vm is not null)
                .Select(vm => vm!)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private Option<PointerPressedEventArgs> _dragPointerPressedEventArgs;
    private bool _dragStarted = false;

    private void PopulateFromViewModel(
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        view.HierarchyGrid.ViewModel = viewModel.HierarchyGridViewModel;

        viewModel
            .ShowSettingsInteraction.RegisterHandler(async ctx =>
            {
                var cvm = new TimeSeriesViewerConfigurationViewModel();
                view.DialogContent.Content = new TimeSeriesViewerConfigurationView()
                {
                    ViewModel = cvm
                };
                viewModel.IsDialogOpen = true;
                await cvm.Result;
                ctx.SetOutput(RxVoid.Default);
                viewModel.IsDialogOpen = false;
                view.DialogContent.Content = null;
            })
            .DisposeWith(disposables);

        viewModel
            .GetDisaggregationRequestInteraction.RegisterHandler(async ctx =>
            {
                var dovm = new DisaggregationOptionsViewModel();
                view.DialogContent.Content = new DisaggregationOptionsView() { ViewModel = dovm };
                viewModel.IsDialogOpen = true;
                var res = await dovm.Result;
                ctx.SetOutput(res);
                viewModel.IsDialogOpen = false;
                view.DialogContent.Content = null;
            })
            .DisposeWith(disposables);

        viewModel
            .RenameSeriesInteraction.RegisterHandler(async ctx =>
            {
                var rsvm = new RenameSeriesViewModel() { CurrentName = ctx.Input.Label };
                view.DialogContent.Content = new RenameSeriesView() { ViewModel = rsvm };
                viewModel.IsDialogOpen = true;
                var res = await rsvm.Result;
                ctx.SetOutput(res);
                viewModel.IsDialogOpen = false;
                view.DialogContent.Content = null;
            })
            .DisposeWith(disposables);

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
                ctx.SetOutput(RxVoid.Default);
            })
            .DisposeWith(disposables);

        HandleDragDrop(view, viewModel, disposables);

        /* Mouse click */
        Signal
            .FromEventPattern<EventHandler<PointerPressedEventArgs>, PointerPressedEventArgs>(
                handler => view.CartesianChart.PointerPressed += handler,
                handler => view.CartesianChart.PointerPressed -= handler
            )
            .Subscribe(t =>
            {
                var args = t.EventArgs;

                /* Only update selection from left click and if selection mode allows it */
                if (
                    !args.Properties.IsLeftButtonPressed
                    || viewModel.SeriesSelectionMode == SelectionMode.None
                )
                    return;

                var chart = (CartesianChart)t.Sender!;
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

                viewModel.Selection = viewModel.SeriesSelectionMode switch
                {
                    SelectionMode.Single
                        => viewModel
                            .Selection.Clear()
                            .Add((Identifier)found[0].Context.Series.Tag!),
                    SelectionMode.Multiple
                        => viewModel.Selection.TryAdd((Identifier)found[0].Context.Series.Tag!),
                    _ => viewModel.Selection,
                };

                if (!viewModel.Selection.IsEmpty)
                {
                    _dragPointerPressedEventArgs = args;
                    _dragStarted = false;
                }
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler => view.CartesianChart.PointerReleased += handler,
                handler => view.CartesianChart.PointerReleased -= handler
            )
            .Subscribe(t =>
            {
                _dragStarted = false;
                _dragPointerPressedEventArgs = Option<PointerPressedEventArgs>.None;
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler => view.CartesianChart.PointerMoved += handler,
                handler => view.CartesianChart.PointerMoved -= handler
            )
            .SubscribeAsync(async t =>
            {
                var args = t.EventArgs;
                if (
                    args.Properties.IsLeftButtonPressed
                    && !viewModel.Selection.IsEmpty
                    && !_dragStarted
                )
                {
                    await _dragPointerPressedEventArgs.IfSomeAsync(async evt =>
                    {
                        var delta = args.GetPosition(view) - evt.GetPosition(view);
                        if (delta is { X: < 5, Y: < 5 })
                            return;

                        _dragStarted = true;

                        var dragData = new DataTransfer();
                        // TODO (see https://docs.avaloniaui.net/docs/how-to/drag-and-drop-how-to)
                        dragData.Add(DataTransferItem.CreateText(viewModel.SelectedSeries.ToCsv()));

                        dragData.Add(
                            DataTransferItem.Create(
                                DataFormat.CreateBytesApplicationFormat("jedplot.timeseries"),
                                viewModel.SelectedSeries.ToBytes()
                            )
                        );

                        var result = await DragDrop.DoDragDropAsync(
                            evt,
                            dragData,
                            DragDropEffects.Copy
                        );

                        _dragStarted = false;
                        _dragPointerPressedEventArgs = Option<PointerPressedEventArgs>.None;
                    });
                }
            })
            .DisposeWith(disposables);

        /* Mouse over chart */
        Signal
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

            ctx.SetOutput(RxVoid.Default);
        });
    }

    private static void HandleDragDrop(
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        Signal
            .FromEventPattern<EventHandler<DragEventArgs>, DragEventArgs>(
                handler => DragDrop.AddDragOverHandler(view.GridMainDisplay, handler),
                handler => DragDrop.AddDragOverHandler(view.GridMainDisplay, handler)
            )
            .Subscribe(t =>
            {
                var args = t.EventArgs;
                args.DragEffects =
                    args.DataTransfer.Formats.Contains(DataFormat.Text)
                    || args.DataTransfer.Formats.Contains(DataFormat.File)
                        ? DragDropEffects.Copy
                        : DragDropEffects.None;
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<DragEventArgs>, DragEventArgs>(
                handler => DragDrop.AddDropHandler(view.GridMainDisplay, handler),
                handler => DragDrop.RemoveDropHandler(view.GridMainDisplay, handler)
            )
            .Subscribe(t =>
            {
                var e = t.EventArgs;

                var bytes = e.DataTransfer.TryGetValue(
                    DataFormat.CreateBytesApplicationFormat("jedplot.timeseries")
                );

                if (bytes?.Length > 0)
                {
                    viewModel.Add(bytes.ToTimeSeriesInfo());
                    return;
                }

                var text = e.DataTransfer.TryGetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var series = text.FromCsv();
                    if (series.Length > 0)
                    {
                        viewModel.Add(series);
                        return;
                    }
                }

                if (e.DataTransfer.TryGetFiles() is { } files)
                {
                    // TODO: check csv, check xlsx, check open office
                    foreach (var file in files)
                    {
                        var path = file.Path.LocalPath;
                        // Process the file
                        viewModel.Log().Debug(path);
                    }
                }
            })
            .DisposeWith(disposables);
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
                grid.RowDefinitions = RowDefinitions.Parse("2*,Auto,*");
                break;

            case DisplayMode.BothHorizontal:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("2*,Auto,*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;

            default:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;
        }
    }
}
