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
    public double DefaultStatusBarHeight { get; set; } = 35d;

    [ObservableAsProperty]
    public partial double StatusBarHeight { get; }

    [ObservableAsProperty]
    public partial double StatusBarOpacity { get; }

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

    [ObservableAsProperty]
    public partial Color ColorAO { get; }

    [ObservableAsProperty]
    public partial Color ColorTC { get; }

    [ObservableAsProperty]
    public partial Color ColorLS { get; }

    [ObservableAsProperty]
    public partial Color ColorSO { get; }

    [ObservableAsProperty]
    public partial OutliersHighlight OutliersHighlight { get; }

    [ObservableAsProperty]
    public partial LegendDisplay Legend { get; }

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

        _colorAOHelper = settingsChange
            .Map(x => x.ColorAO)
            .ToProperty(
                this,
                x => x.ColorAO,
                initialValue: TimeSeriesViewerSettings.DefaultColorAO,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _colorTCHelper = settingsChange
            .Map(x => x.ColorTC)
            .ToProperty(
                this,
                x => x.ColorTC,
                initialValue: TimeSeriesViewerSettings.DefaultColorTC,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _colorLSHelper = settingsChange
            .Map(x => x.ColorLS)
            .ToProperty(
                this,
                x => x.ColorLS,
                initialValue: TimeSeriesViewerSettings.DefaultColorLS,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _colorSOHelper = settingsChange
            .Map(x => x.ColorSO)
            .ToProperty(
                this,
                x => x.ColorSO,
                initialValue: TimeSeriesViewerSettings.DefaultColorSO,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _outliersHighlightHelper = this.WhenAnyValue(
                x => x.ColorAO,
                x => x.ColorTC,
                x => x.ColorLS,
                x => x.ColorSO
            )
            .Select(t =>
            {
                var (ao, tc, ls, so) = t;
                return new OutliersHighlight(ao, tc, ls, so);
            })
            .ToProperty(this, x => x.OutliersHighlight, scheduler: RxSchedulers.MainThreadScheduler)
            .DisposeWith(disposables);

        _legendHelper = settingsChange
            .Map(x => x.Legend)
            .ToProperty(
                this,
                x => x.Legend,
                initialValue: LegendDisplay.Hidden,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);
    }

    private void HandleStatusBarVisibility(MultipleDisposable disposables)
    {
        this.WhenAnyValue(x => x.Configuration)
            .Map(o => o.Match(Signal.Return, Signal.Empty<TimeSeriesViewerSettings>))
            .SwitchTo()
            .Select(c => c.ShowStatusBar)
            .StartWith(false)
            .SubscribePrimitives(b =>
            {
                ShowStatusBar = b;
            })
            .DisposeWith(disposables);

        _statusBarHeightHelper = this.WhenAnyValue(x => x.ShowStatusBar)
            .CombineLatest(
                this.WhenAnyValue(x => x.IsWorking),
                this.WhenAnyValue(x => x.TempShowStatusBar)
            )
            .Select(t => t.First || t.Second || t.Third)
            .Throttle(TimeSpan.FromMilliseconds(20))
            .DistinctUntilChanged()
            .Select(b => b ? DefaultStatusBarHeight : 0d)
            .ToProperty(
                this,
                x => x.StatusBarHeight,
                initialValue: DefaultStatusBarHeight,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);

        _statusBarOpacityHelper = this.WhenAnyValue(x => x.ShowStatusBar)
            .CombineLatest(
                this.WhenAnyValue(x => x.IsWorking),
                this.WhenAnyValue(x => x.TempShowStatusBar)
            )
            .Select(t => t.First || t.Second || t.Third)
            .Throttle(TimeSpan.FromMilliseconds(20))
            .DistinctUntilChanged()
            .Select(b => b ? 1d : 0d)
            .ToProperty(
                this,
                x => x.StatusBarOpacity,
                initialValue: 1d,
                scheduler: RxSchedulers.MainThreadScheduler
            )
            .DisposeWith(disposables);
    }
}
