using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using JedPlotUtils.ScottPlot.Common.Components;
using JedPlotUtils.Scottplot.Common.Components.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using AV = Avalonia;

namespace JedPlotUtils.ScottPlot.Avalonia.Components.Views;

public partial class TimeSeriesViewerView : ReactiveUserControl<TimeSeriesViewerViewModel>
{
    private PlotConfiguration _splotConfig;
    private PlotInteractivity? _splotInteractivity;

    public TimeSeriesViewerView()
    {
        InitializeComponent();

        ComboBoxDisplayMode.ItemsSource = Enum.GetValues<DisplayMode>().ToSeq();

        _splotConfig = new() { };
        Chart.Plot.ConfigurePlot(_splotConfig);

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
        TimeSeriesViewerView view,
        TimeSeriesViewerViewModel viewModel,
        CompositeDisposable disposables
    )
    {
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
                ctx.SetOutput(Unit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .DrawChartInteraction.RegisterHandler(ctx =>
            {
                view._splotInteractivity = view.Chart.DrawScatterLines(
                    view._splotConfig.PlotRender,
                    ctx.Input
                );

                ctx.SetOutput(Unit.Default);
            })
            .DisposeWith(disposables);

        HandleMouseEvents(view, disposables);
    }

    private static void HandleMouseEvents(
        TimeSeriesViewerView view,
        CompositeDisposable disposables
    )
    {
        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                _ =>
                    (_, args) =>
                    {
                        if (view._splotInteractivity is not null)
                            view.Chart.HandleMouseOver(args, view._splotInteractivity.Value);
                    },
                handler => view.Chart.PointerMoved += handler,
                handler => view.Chart.PointerMoved -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                _ =>
                    (_, args) =>
                    {
                        if (view._splotInteractivity is not null)
                            view.Chart.HandleMouseLeft(args, view._splotInteractivity.Value);
                    },
                handler => view.Chart.PointerExited += handler,
                handler => view.Chart.PointerExited -= handler
            )
            .ObserveOn(RxApp.MainThreadScheduler)
            .Subscribe()
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerPressedEventArgs>, PointerPressedEventArgs>(
                o =>
                    (o, args) =>
                    {
                        if (view._splotInteractivity is not null)
                        {
                            view._splotInteractivity = view.Chart.SeriesSelection(
                                args,
                                view._splotInteractivity.Value
                            );
                        }
                    },
                handler => view.Chart.PointerPressed += handler,
                handler => view.Chart.PointerPressed -= handler
            )
            .Subscribe()
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
