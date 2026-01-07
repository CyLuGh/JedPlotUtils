using System;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using Avalonia.Controls;
using Avalonia.Input;
using DemoData;
using JedPlotUtils.ScottPlot;
using JedPlotUtils.ScottPlot.Avalonia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ScottPlot.Plottables;
using RxUnit = System.Reactive.Unit;

namespace AvaloniaApplication;

public partial class TimeSeriesSampleView : ReactiveUserControl<TimeSeriesSampleViewModel>
{
    private PlotConfiguration _splotConfig;
    private PlotInteractivity? _splotInteractivity;

    public TimeSeriesSampleView()
    {
        InitializeComponent();

        _splotConfig = new()
        {
            Palette = new ScottPlot.Palettes.DarkPastel(),
            PlotRender = new()
            {
                InteractivityMode = InteractivityMode.AllSeries,
                UseDateTimeAxis = true,
                XFormatter = d => DateTime.FromOADate(d).ToString("yyyy-MM"),
                YFormatter = d => d.ToString()
            },
            LegendLayout = new LegendLayout()
            {
                Edge = ScottPlot.Edge.Bottom,
                Orientation = ScottPlot.Orientation.Horizontal
            }
        };
        SPlot.Plot.ConfigurePlot(_splotConfig);

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
        TimeSeriesSampleView view,
        TimeSeriesSampleViewModel viewModel,
        CompositeDisposable disposables
    )
    {
        viewModel
            .DrawChartInteraction.RegisterHandler(ctx =>
            {
                view._splotInteractivity = view.SPlot.DrawScatterLines(
                    view._splotConfig.PlotRender,
                    ctx.Input
                ) with
                {
                    PlotSelection = new PlotSelection()
                    {
                        SelectionMode = JedPlotUtils.ScottPlot.SelectionMode.Single
                    }
                };
                ctx.SetOutput(RxUnit.Default);
            })
            .DisposeWith(disposables);

        Observable
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                _ =>
                    (_, args) =>
                    {
                        if (view._splotInteractivity is not null)
                        {
                            var sIndex = view.SPlot.HandleMouseOver(
                                args,
                                view._splotInteractivity.Value
                            );
                        }
                    },
                handler => view.SPlot.PointerMoved += handler,
                handler => view.SPlot.PointerMoved -= handler
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
                            view.SPlot.HandleMouseLeft(args, view._splotInteractivity.Value);
                    },
                handler => view.SPlot.PointerExited += handler,
                handler => view.SPlot.PointerExited -= handler
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
                            view._splotInteractivity = view.SPlot.SeriesSelection(
                                args,
                                view._splotInteractivity.Value
                            );
                        }
                    },
                handler => view.SPlot.PointerPressed += handler,
                handler => view.SPlot.PointerPressed -= handler
            )
            .Subscribe()
            .DisposeWith(disposables);
    }
}
