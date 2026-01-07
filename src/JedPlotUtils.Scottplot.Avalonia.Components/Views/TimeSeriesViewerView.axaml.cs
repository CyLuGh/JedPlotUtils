using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using JedPlotUtils.ScottPlot.Avalonia;
using JedPlotUtils.ScottPlot.Common.Components;
using JedPlotUtils.Scottplot.Common.Components.ViewModels;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot.Plottables;
using AV = Avalonia;
using RxUnit = System.Reactive.Unit;

namespace JedPlotUtils.ScottPlot.Avalonia.Components.Views;

public partial class TimeSeriesViewerView : ReactiveUserControl<TimeSeriesViewerViewModel>
{
    private PlotConfiguration _splotConfig;
    private PlotInteractivity? _splotInteractivity;

    public TimeSeriesViewerView()
    {
        InitializeComponent();

        ComboBoxDisplayMode.ItemsSource = Enum.GetValues<DisplayMode>().ToSeq();

        _splotConfig = new()
        {
            PlotRender = new() { InteractivityMode = InteractivityMode.SingleSeries }
        };
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
                ctx.SetOutput(RxUnit.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .DrawChartInteraction.RegisterHandler(ctx =>
            {
                var res = view.Chart.DrawScatterLines(view._splotConfig.PlotRender, ctx.Input);
                view._splotInteractivity = res;

                ctx.SetOutput(res.Series);
            })
            .DisposeWith(disposables);

        viewModel
            .HighlightChartInteraction.RegisterHandler(ctx =>
            {
                var odt = ctx.Input;

                odt.IfNone(() =>
                {
                    view._splotInteractivity?.Decorations.Hide();
                    view.Chart.Refresh();
                });

                odt.IfSome(t =>
                {
                    var (scatter, period) = t;
                    var oa = period.ToDateTime(TimeOnly.MinValue).ToOADate();
                    var point = scatter.Data.GetScatterPoints().Find(c => c.X.Equals(oa));

                    point
                        .Some(coord =>
                        {
                            view.Chart.Plot.HighlightPoint(
                                view._splotInteractivity!.Value,
                                coord,
                                scatter,
                                new(oa, coord.Y, -1)
                            );
                            view.Chart.Refresh();
                        })
                        .None(() =>
                        {
                            view._splotInteractivity?.Decorations.Hide();
                            view.Chart.Refresh();
                        });

                    //var point = series
                    //    .Data.GetScatterPoints()
                    //    .Find(c => c.X.Equals(period.ToOADate()));
                    //point
                    //    .Some(coord =>
                    //    {
                    //        view._linkedInteractivity.HighlightPoint(series, coord);

                    //        view._linkedInteractivity.ShowText(
                    //            view.LinkedAvaPlot,
                    //            coord,
                    //            coord,
                    //            view._linkedInteractivity.IsTimeSeries
                    //                ? $"{series.LegendText} - {DateTime.FromOADate(coord.X):yyyy-MM-dd}: {coord.Y:N}"
                    //                : $"{series.LegendText} - {coord.X:0}: {coord.Y:N}",
                    //            series.MarkerStyle.FillColor
                    //        );

                    //        view.LinkedAvaPlot.Refresh();
                    //    })
                    //    .None(() =>
                    //    {
                    //        view.LinkedAvaPlot.HideDecorations(view._linkedInteractivity);
                    //        view.LinkedAvaPlot.Refresh();
                    //    });
                });

                ctx.SetOutput(RxUnit.Default);
            })
            .DisposeWith(disposables);

        HandleMouseEvents(view, viewModel, disposables);
    }

    private static void HandleMouseEvents(
        TimeSeriesViewerView view,
        TimeSeriesViewerViewModel viewModel,
        CompositeDisposable disposables
    )
    {
        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                _ =>
                    (_, args) =>
                    {
                        if (view._splotInteractivity is not null)
                        {
                            var si = view.Chart.HandleMouseOver(
                                args,
                                view._splotInteractivity.Value
                            );

                            viewModel.HoveredPoint = si.NearestPoint.Match(
                                np =>
                                    (
                                        viewModel.Series.Keys.ToSeq()[si.Index],
                                        DateOnly.FromDateTime(DateTime.FromOADate(np.Coordinates.X))
                                    ),
                                () => Option<(Scatter, DateOnly)>.None
                            );
                        }
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
                        {
                            view.Chart.HandleMouseLeft(args, view._splotInteractivity.Value);
                            viewModel.HoveredPoint = Option<(Scatter, DateOnly)>.None;
                        }
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
