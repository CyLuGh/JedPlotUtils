using JDPlus.WS.Client;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    public partial DisplayMode DisplayMode { get; }

    [ObservableAsProperty]
    public partial Option<Func<DateOnly, string>> DateFormatter { get; }

    [ObservableAsProperty]
    public partial string NumberFormat { get; }

    [ObservableAsProperty]
    public partial IPalette Palette { get; }

    [ObservableAsProperty]
    public partial SelectionMode SeriesSelectionMode { get; }

    [ObservableAsProperty]
    public partial bool SwitchPositions { get; }

    [ObservableAsProperty]
    public partial bool AutoConnect { get; }

    [ObservableAsProperty]
    public partial bool AutoScrollGrid { get; }

    [ObservableAsProperty]
    public partial string RemoteServerAddress { get; }

    private void HandleConnectionObservableAsProperties()
    {
        _hasConnectionHelper = this.WhenAnyValue(x => x.WsManager)
            .Select(o => o.IsRight)
            .ToProperty(this, x => x.HasConnection, scheduler: RxSchedulers.MainThreadScheduler);

        _remoteServerAddressHelper = this.WhenAnyValue(x => x.WsManager)
            .Map(o => o.Match(Signal.Return, _ => Signal.Empty<CommunicationManager>()))
            .SwitchTo()
            .Map(cm => cm.RemoteServerAddress)
            .ToProperty(this, x => x.RemoteServerAddress);
    }

    private void HandleSettingsObservableAsProperties(
        IObservable<TimeSeriesViewerSettings> settingsChange,
        MultipleDisposable disposables
    )
    {
        _seriesSelectionModeHelper = settingsChange
            .Map(x => x.SeriesSelectionMode)
            .ToProperty(
                this,
                x => x.SeriesSelectionMode,
                initialValue: SelectionMode.Single,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _displayModeHelper = settingsChange
            .Map(x => x.DisplayMode)
            .ToProperty(
                this,
                x => x.DisplayMode,
                initialValue: DisplayMode.BothHorizontal,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _paletteHelper = settingsChange
            .Map(x => x.Palette.ToPalette())
            .ToProperty(
                this,
                x => x.Palette,
                initialValue: Palettes.Available[0],
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _dateFormatterHelper = settingsChange
            .Map(settings =>
                !string.IsNullOrWhiteSpace(settings.DateFormat)
                    ? Option<Func<DateOnly, string>>.Some(d => d.ToString(settings.DateFormat))
                    : Option<Func<DateOnly, string>>.None
            )
            .ToProperty(this, x => x.DateFormatter, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(disposables);

        _numberFormatHelper = settingsChange
            .Map(settings =>
            {
                char format = settings.HasThousandsSeparators ? 'N' : 'F';
                return $"{format}{settings.Decimals}";
            })
            .ToProperty(
                this,
                x => x.NumberFormat,
                initialValue: "F0",
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _switchPositionsHelper = settingsChange
            .Map(settings => settings.SwitchGridAndChart)
            .ToProperty(
                this,
                x => x.SwitchPositions,
                initialValue: false,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _autoConnectHelper = settingsChange
            .Map(x => x.AutoConnect)
            .ToProperty(this, x => x.AutoConnect, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(disposables);

        _autoScrollGridHelper = settingsChange
            .Map(x => x.AutoScrollGrid)
            .ToProperty(this, x => x.AutoScrollGrid, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(disposables);
    }
}
