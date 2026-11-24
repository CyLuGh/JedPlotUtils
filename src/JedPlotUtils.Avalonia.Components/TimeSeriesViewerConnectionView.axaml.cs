using Avalonia.Input;
using JedPlotUtils.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.Avalonia.Components;

public partial class TimeSeriesViewerConnectionView
    : ReactiveUserControl<TimeSeriesViewerConfigurationViewModel>
{
    public TimeSeriesViewerConnectionView()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(x => x is not null)
                .Map(x => x!)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .SubscribePrimitives()
                .DisposeWith(disposables);
        });
    }

    private static void PopulateFromViewModel(
        TimeSeriesViewerConnectionView view,
        TimeSeriesViewerConfigurationViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        Signal
            .FromEventPattern<KeyEventArgs>(
                handler => view.KeyDown += handler,
                handler => view.KeyDown -= handler
            )
            .SubscribePrimitives(t =>
            {
                var args = t.EventArgs;
                switch (args.Key)
                {
                    case Key.Enter:
                        Signal
                            .Return(viewModel.PersistSettings)
                            .InvokeCommand(viewModel, x => x.SaveCommand);
                        break;

                    case Key.Escape:
                        Signal
                            .Return(RxVoid.Default)
                            .InvokeCommand(viewModel, x => x.CancelCommand);
                        break;
                }
            })
            .DisposeWith(disposables);
    }
}
