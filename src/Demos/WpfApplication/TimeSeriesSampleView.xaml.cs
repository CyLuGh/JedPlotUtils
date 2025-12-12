using System.Reactive.Disposables;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using JedPlotUtils.ScottPlot;
using JedPlotUtils.ScottPlot.WPF;
using ReactiveMarbles.ObservableEvents;
using ReactiveUI;
using RxUnit = System.Reactive.Unit;

namespace WpfApplication
{
    public partial class TimeSeriesSampleView : ReactiveUserControl<TimeSeriesSampleViewModel>
    {
        private PlotConfiguration _splotConfig;
        private PlotInteractivity? _splotInteractivity;

        public TimeSeriesSampleView()
        {
            InitializeComponent();

            _splotConfig = new();
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
                    );
                    ctx.SetOutput(RxUnit.Default);
                })
                .DisposeWith(disposables);

            view.BindCommand(viewModel, vm => vm.BuildSampleCommand, v => v.ButtonSample)
                .DisposeWith(disposables);

            view.SPlot.Events()
                .MouseMove.Subscribe(evt =>
                {
                    if (view._splotInteractivity is not null)
                        view.SPlot.HandleMouseOver(evt, view._splotInteractivity.Value);
                })
                .DisposeWith(disposables);

            view.SPlot.Events()
                .MouseLeave.Subscribe(evt =>
                {
                    if (view._splotInteractivity is not null)
                        view.SPlot.HandleMouseLeft(evt, view._splotInteractivity.Value);
                })
                .DisposeWith(disposables);
        }
    }
}
