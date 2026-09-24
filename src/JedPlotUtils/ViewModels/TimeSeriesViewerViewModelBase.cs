global using RxCommand = ReactiveUI.ReactiveCommand<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;
global using RxInteraction = ReactiveUI.Interaction<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;
using System.Globalization;
using HierarchyGrid.Definitions;
using JDPlus.WS.Client;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using ReactiveUI.SourceGenerators;
using Splat;
using SelectionMode = JedPlotUtils.Models.SelectionMode;

namespace JedPlotUtils.ViewModels;

public abstract partial class TimeSeriesViewerViewModelBase : BaseViewModel
{
    [Reactive]
    public partial bool IsDialogOpen { get; set; }

    [Reactive]
    public partial double DialogOpacity { get; set; }

    [Reactive]
    protected partial HashMap<Identifier, TimeSeriesInfo> SeriesCache { get; set; }

    public HierarchyGridViewModel HierarchyGridViewModel { get; } = new();

    [Reactive]
    public partial IPalette Palette { get; set; } = Palettes.Available[0];

    [Reactive]
    public partial SelectionMode SeriesSelectionMode { get; set; } = SelectionMode.Single;

    [Reactive]
    public partial LanguageExt.HashSet<Identifier> Selection { get; set; }

    /// <summary>
    /// The point that is currently hovered in the chart or the grid.
    /// </summary>
    [Reactive]
    public partial Option<(Identifier, DateOnly)> HoveredPoint { get; set; }

    [ObservableAsProperty]
    private Option<(Identifier, DateOnly)> _throttledHoveredPoint;

    public ReactiveCommand<Option<(Identifier, DateOnly)>, bool> HoverCellGridCommand { get; }
    public ReactiveCommand<
        Option<(Identifier, DateOnly)>,
        RxVoid
    > HighlightChartPointCommand { get; }

    public ReactiveCommand<
        (Seq<TimeSeriesInfo>, Func<DateOnly, string>, Func<double, string>),
        HierarchyDefinitions
    > BuildHierarchyGridDefinitions { get; }

    public Interaction<
        Option<(Identifier, DateOnly)>,
        RxVoid
    > HighlightChartPointInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }
    public ReactiveCommand<DisplayMode, RxVoid> AdaptDisplayModeCommand { get; }
    public Interaction<DisplayMode, RxVoid> AdaptDisplayModeInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    [Reactive]
    public partial Option<Func<DateOnly, string>> DateFormatter { get; set; }

    [Reactive]
    public partial string NumberFormat { get; set; }

    public ReactiveCommand<Func<DateOnly, string>, Unit> AdaptXAxisCommand { get; }
    public Interaction<Func<DateOnly, string>, Unit> AdaptXAxisInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    [Reactive]
    public partial TimeSeriesViewerSettings Configuration { get; set; }

    [ObservableAsProperty(ReadOnly = false)]
    private Option<CommunicationManager> _wsManager;

    [ObservableAsProperty(ReadOnly = false)]
    private bool _isConnecting;

    [ObservableAsProperty]
    private bool _hasConnection;

    public ReactiveCommand<string?, Option<CommunicationManager>> GetConnectionCommand { get; }

    public ReactiveCommand<LanguageExt.HashSet<Identifier>, RxVoid> ToggleHighlightsCommand { get; }

    [ObservableAsProperty]
    private Seq<TimeSeriesInfo> _selectedSeries;

    public RxCommand ClearDerivedCommand { get; }

    public Interaction<TimeSeriesInfo, string?> RenameSeriesInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public ReactiveCommand<Seq<TimeSeriesInfo>, RxVoid> RenameSeriesCommand { get; }
    public ReactiveCommand<SeriesChartType, RxVoid> ChangeSeriesChartTypesCommand { get; }

    public ReactiveCommand<TimeSeriesViewerSettings, RxVoid> ShowSettingsCommand { get; }
    public Interaction<
        TimeSeriesViewerSettings,
        Option<TimeSeriesViewerSettings>
    > ShowSettingsInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<TimeSeriesViewerSettings, string?> ShowConnectionSettingsCommand { get; }
    public Interaction<
        TimeSeriesViewerSettings,
        Option<TimeSeriesViewerSettings>
    > ShowConnectionSettingsInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<LanguageExt.HashSet<Identifier>, RxVoid> RemoveSelectionCommand { get; }
    public RxCommand ClearSeriesCommand { get; }

    public RxCommand CopyToClipboardCommand { get; }
    public Interaction<Seq<TimeSeriesInfo>, RxVoid> CopyToClipboardInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public RxCommand PasteFromClipboardCommand { get; }
    public Interaction<RxVoid, Seq<TimeSeriesInfo>> PasteFromClipboardInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    public RxInteraction ClearInfoInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    protected TimeSeriesViewerViewModelBase()
    {
        ClearInfoInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));

        ShowSettingsCommand = CreateCommandShowSettingsCommand();
        ShowConnectionSettingsCommand = CreateCommandShowConnectionSettingsCommand();
        ClearDerivedCommand = ReactiveCommand.CreateFromTask(async () => await Clear(true));
        ClearSeriesCommand = ReactiveCommand.CreateFromTask(async () => await Clear());

        AdaptDisplayModeCommand = CreateCommandAdaptDisplayModeCommand();
        HoverCellGridCommand = CreateCommandHoverGridCommand();
        HighlightChartPointCommand = CreateHighlightPointCommand();
        BuildHierarchyGridDefinitions = CreateCommandBuildHierarchyGridDefinitions();
        AdaptXAxisCommand = CreateCommandAdaptXAxisCommand();

        GetConnectionCommand = CreateCommandGetConnection(ShowConnectionSettingsCommand);
        ToggleHighlightsCommand = CreateCommandToggleHighlights();

        DisaggregateCommand = CreateCommandDisaggregateCommand(
            ClearSeriesCommand,
            ClearDerivedCommand
        );
        CreateDisaggregatedSeriesCommand = CreateCommandCreateDisaggregatedSeriesCommand();
        ChangeFrequencyCommand = CreateCommandChangeFrequencyCommand();

        RenameSeriesCommand = CreateCommandRenameSeriesCommand();
        RemoveSelectionCommand = CreateCommandRemoveSelectionCommand();

        ChangeSeriesChartTypesCommand = CreateCommandChangeSeriesChartTypesCommand();
        CopyToClipboardCommand = CreateCommandCopyToClipboardCommand();
        PasteFromClipboardCommand = CreateCommandPasteFromClipboardCommand();

        ShowDisaggregationInfo = CreateCommandShowDisaggregationInfo();

        _hasConnectionHelper = this.WhenAnyValue(x => x.WsManager)
            .Select(o => o.IsSome)
            .ToProperty(this, x => x.HasConnection, scheduler: RxSchedulers.MainThreadScheduler);

        _selectedSeriesHelper = this.WhenAnyValue(x => x.Selection)
            .Select(sel => SeriesCache.Values.Where(x => sel.Contains(x.Identifier)).ToSeq())
            .ToProperty(this, x => x.SelectedSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _throttledHoveredPointHelper = this.WhenAnyValue(x => x.HoveredPoint)
            .Throttle(TimeSpan.FromMilliseconds(100))
            .DistinctUntilChanged()
            .ToProperty(
                this,
                x => x.ThrottledHoveredPoint,
                scheduler: RxSchedulers.MainThreadScheduler
            );

        _hasDisaggregationResultsHelper = this.WhenAnyValue(x => x.DisaggregationResults)
            .Select(o => o.IsSome)
            .ToProperty(
                this,
                x => x.HasDisaggregationResults,
                scheduler: RxSchedulers.MainThreadScheduler
            );

        this.WhenActivated(disposables =>
        {
            HierarchyGridViewModel
                .WhenAnyValue(x => x.HoveredCell)
                .Throttle(TimeSpan.FromMilliseconds(50))
                .DistinctUntilChanged()
                .Subscribe(o =>
                {
                    HoveredPoint = o.Match(
                        pc =>
                        {
                            if (
                                pc.ConsumerDefinition.Tag is DateOnly period
                                && pc.ProducerDefinition.Tag is Identifier identifier
                            )
                            {
                                return (identifier, period);
                            }

                            return Option<(Identifier, DateOnly)>.None;
                        },
                        () => Option<(Identifier, DateOnly)>.None
                    );
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.SeriesSelectionMode)
                .Subscribe(_ => Selection = LanguageExt.HashSet<Identifier>.Empty)
                .DisposeWith(disposables);

            Signal
                .Return(TimeSeriesViewerConfigurationViewModel.Load())
                .DistinctUntilChanged()
                .Subscribe(settings =>
                {
                    Signal.Return(settings.WebServiceAddress).InvokeCommand(GetConnectionCommand);
                    Configuration = settings;
                    HierarchyGridViewModel.Theme = new LightGridTheme();
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.Configuration)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .Subscribe(ApplySettings)
                .DisposeWith(disposables);

            HierarchyGridViewModel
                .WhenAnyValue(x => x.Producers)
                .Select(ps =>
                {
                    var selected = ps.Select(p =>
                            p.WhenAnyValue(x => x.IsHighlighted)
                                .DistinctUntilChanged()
                                .Where(x => x)
                                .Select(_ => (Identifier)p.Tag!)
                        )
                        .Merge();

                    return selected;
                })
                .Switch()
                .Do(_ =>
                {
                    if (SeriesSelectionMode != SelectionMode.None)
                        return;

                    foreach (var p in HierarchyGridViewModel.Producers)
                        p.IsHighlighted = false;
                })
                .Subscribe(x =>
                {
                    Selection = SeriesSelectionMode switch
                    {
                        SelectionMode.Single => Selection.Clear().AddOrUpdate(x),
                        SelectionMode.Multiple => Selection.AddOrUpdate(x),
                        _ => Selection,
                    };
                });

            HierarchyGridViewModel
                .WhenAnyValue(x => x.Producers)
                .Select(ps =>
                {
                    var selected = ps.Select(p =>
                            p.WhenAnyValue(x => x.IsHighlighted)
                                .DistinctUntilChanged()
                                .Where(x => !x)
                                .Select(_ => (Identifier)p.Tag!)
                        )
                        .Merge();

                    return selected;
                })
                .Switch()
                .Subscribe(x =>
                {
                    Selection = Selection.Remove(x);
                });

            this.WhenAnyValue(x => x.NumberFormat).Subscribe(UpdateFormat).DisposeWith(disposables);
        });
    }

    private RxCommand CreateCommandPasteFromClipboardCommand()
    {
        PasteFromClipboardInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(Seq<TimeSeriesInfo>.Empty)
        );
        var cmd = ReactiveCommand.CreateFromTask(async () =>
        {
            var series = await PasteFromClipboardInteraction.Handle(RxVoid.Default);
            Add(series);
        });
        return cmd;
    }

    private RxCommand CreateCommandCopyToClipboardCommand()
    {
        CopyToClipboardInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var canExecute = this.WhenAnyValue(x => x.SelectedSeries)
            .Select(sel => !sel.IsEmpty)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.CreateFromObservable(
            () => CopyToClipboardInteraction.Handle(SelectedSeries),
            canExecute
        );
        cmd.ThrownExceptions.Subscribe(ex => this.Log().Error("Failed to copy to clipboard", ex));
        return cmd;
    }

    private ReactiveCommand<SeriesChartType, RxVoid> CreateCommandChangeSeriesChartTypesCommand()
    {
        var canExecute = this.WhenAnyValue(x => x.Selection)
            .Select(sel => sel.Count > 0)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.CreateRunInBackground(
            (SeriesChartType sct) =>
            {
                foreach (var id in Selection)
                    ChangeSeriesChartType(id, sct);
            },
            canExecute
        );
        cmd.ThrownExceptions.Subscribe(ex =>
            this.Log().Error("Failed to paste from clipboard", ex)
        );

        return cmd;
    }

    private ReactiveCommand<
        TimeSeriesViewerSettings,
        string?
    > CreateCommandShowConnectionSettingsCommand()
    {
        ShowConnectionSettingsInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(Option<TimeSeriesViewerSettings>.None)
        );
        var cmd = ReactiveCommand.CreateFromTask(
            async (TimeSeriesViewerSettings settings) =>
            {
                var config = await ShowConnectionSettingsInteraction.Handle(settings);
                config.IfSome(c =>
                {
                    Configuration = c;
                });

                return config.Match(s => s.WebServiceAddress, () => string.Empty);
            }
        );
        return cmd;
    }

    private ReactiveCommand<TimeSeriesViewerSettings, RxVoid> CreateCommandShowSettingsCommand()
    {
        ShowSettingsInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(Option<TimeSeriesViewerSettings>.None)
        );
        var cmd = ReactiveCommand.CreateFromTask(
            async (TimeSeriesViewerSettings settings) =>
            {
                var config = await ShowSettingsInteraction.Handle(settings);
                config.IfSome(c =>
                {
                    Configuration = c;
                });
            }
        );
        return cmd;
    }

    private ReactiveCommand<
        LanguageExt.HashSet<Identifier>,
        RxVoid
    > CreateCommandRemoveSelectionCommand()
    {
        var canExecute = this.WhenAnyValue(x => x.Selection)
            .Select(sel => !sel.IsEmpty)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.Create(
            (LanguageExt.HashSet<Identifier> selection) => Remove(selection),
            canExecute
        );
        return cmd;
    }

    private ReactiveCommand<Seq<TimeSeriesInfo>, RxVoid> CreateCommandRenameSeriesCommand()
    {
        RenameSeriesInteraction.RegisterHandler(ctx => ctx.SetOutput(string.Empty));
        var canExecute = this.WhenAnyValue(x => x.SelectedSeries)
            .Select(sel => sel.Length == 1)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.CreateFromTask(
            async (Seq<TimeSeriesInfo> seq) =>
            {
                await seq.HeadOrNone()
                    .IfSomeAsync(async tsi =>
                    {
                        var newName = await RenameSeriesInteraction.Handle(tsi);
                        RenameSeries(tsi, newName);
                    });
            },
            canExecute
        );
        return cmd;
    }

    private void ApplySettings(TimeSeriesViewerSettings settings)
    {
        SeriesSelectionMode = settings.SeriesSelectionMode;
        DisplayMode = settings.DisplayMode;
        Palette = settings.Palette.ToPalette();
        DateFormatter = !string.IsNullOrWhiteSpace(settings.DateFormat)
            ? Option<Func<DateOnly, string>>.Some(d => d.ToString(settings.DateFormat))
            : Option<Func<DateOnly, string>>.None;
        char format = settings.HasThousandsSeparators ? 'N' : 'F';
        NumberFormat = $"{format}{settings.Decimals}";
    }

    private ReactiveCommand<LanguageExt.HashSet<Identifier>, RxVoid> CreateCommandToggleHighlights()
    {
        var cmd = ReactiveCommand.Create(
            (LanguageExt.HashSet<Identifier> selection) =>
            {
                foreach (var producer in HierarchyGridViewModel.Producers)
                {
                    producer.IsHighlighted =
                        producer.Tag is Identifier identifier && selection.Contains(identifier);
                }
            }
        );

        this.WhenAnyValue(x => x.Selection).DistinctUntilChanged().InvokeCommand(cmd);
        cmd.Select(_ => false).InvokeCommand(HierarchyGridViewModel, x => x.DrawGridCommand);

        return cmd;
    }

    private ReactiveCommand<string?, Option<CommunicationManager>> CreateCommandGetConnection(
        ReactiveCommand<TimeSeriesViewerSettings, string?> connectionSettingsCommand
    )
    {
        var cmd = ReactiveCommand.CreateFromTask(
            async (string? address) =>
            {
                var cm = new CommunicationManager(
                    !string.IsNullOrWhiteSpace(address)
                        ? address
                        : TimeSeriesViewerSettings.DefaultWebServiceAddress
                );
                await cm.GetVersion();
                return Option<CommunicationManager>.Some(cm);
            }
        );

        connectionSettingsCommand
            .Where(address => !string.IsNullOrWhiteSpace(address))
            .InvokeCommand(cmd);

        _isConnectingHelper = cmd.IsExecuting.ToProperty(
            this,
            x => x.IsConnecting,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        _wsManagerHelper = cmd.Merge(
                cmd.ThrownExceptions.Select(_ => Option<CommunicationManager>.None)
            )
            .ToProperty(this, x => x.WsManager, initialValue: Option<CommunicationManager>.None);

        return cmd;
    }

    private ReactiveCommand<Func<DateOnly, string>, Unit> CreateCommandAdaptXAxisCommand()
    {
        AdaptXAxisInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
        var cmd = ReactiveCommand.CreateFromObservable<Func<DateOnly, string>, Unit>(f =>
            AdaptXAxisInteraction.Handle(f)
        );

        this.WhenAnyValue(x => x.DateFormatter)
            .Select(o =>
                o.Match(
                    Signal.Return,
                    () => Signal.Return<Func<DateOnly, string>>(d => d.ToString("yyyy-MM"))
                )
            )
            .Switch()
            .InvokeCommand(cmd);

        return cmd;
    }

    private ReactiveCommand<Option<(Identifier, DateOnly)>, bool> CreateCommandHoverGridCommand()
    {
        /* Grid highlighting must be done on UI thread otherwise it will throw an exception if grid has to scroll to
           an element that is not yet drawn */
        var cmd = ReactiveCommand.Create((Option<(Identifier, DateOnly)> si) => DoHoverGrid(si));

        var hoverObservable = this.WhenAnyValue(x => x.ThrottledHoveredPoint).Publish().RefCount();

        hoverObservable
            .Merge(hoverObservable.CombineLatest(cmd.Where(x => x)).Select(t => t.First))
            //.DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .InvokeCommand(cmd);

        cmd.Where(x => !x)
            .Throttle(TimeSpan.FromMilliseconds(250))
            .InvokeCommand(HierarchyGridViewModel, x => x.DrawGridCommand);

        return cmd;
    }

    private ReactiveCommand<DisplayMode, RxVoid> CreateCommandAdaptDisplayModeCommand()
    {
        AdaptDisplayModeInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (DisplayMode dm) => AdaptDisplayModeInteraction.Handle(dm)
        );
        this.WhenAnyValue(x => x.DisplayMode)
            .Throttle(TimeSpan.FromMilliseconds(20))
            .InvokeCommand(cmd);
        return cmd;
    }

    private ReactiveCommand<Option<(Identifier, DateOnly)>, RxVoid> CreateHighlightPointCommand()
    {
        HighlightChartPointInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var cmd = ReactiveCommand.CreateFromObservable<Option<(Identifier, DateOnly)>, RxVoid>(t =>
            HighlightChartPointInteraction.Handle(t)
        );

        this.WhenAnyValue(x => x.ThrottledHoveredPoint)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(20))
            .InvokeCommand(cmd);

        return cmd;
    }

    private ReactiveCommand<
        (Seq<TimeSeriesInfo>, Func<DateOnly, string>, Func<double, string>),
        HierarchyDefinitions
    > CreateCommandBuildHierarchyGridDefinitions()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            ((Seq<TimeSeriesInfo>, Func<DateOnly, string>, Func<double, string>) t) =>
            {
                var (infos, dateFormatter, numberFormatter) = t;
                return DoBuildHierarchyGridDefinitions(infos, dateFormatter, numberFormatter);
            }
        );

        this.WhenAnyValue(x => x.SeriesCache)
            .Select(hm => hm.Values.ToSeq())
            .CombineLatest(
                this.WhenAnyValue(x => x.DateFormatter)
                    .Select(o => o.Match(f => f, () => d => d.ToString("yyyy-MM"))),
                this.WhenAnyValue(x => x.NumberFormat)
                    .Select<string?, Func<double, string>>(f =>
                        !string.IsNullOrWhiteSpace(f)
                            ? d => d.ToString(f)
                            : d => d.ToString(CultureInfo.InvariantCulture)
                    )
            )
            .Throttle(TimeSpan.FromMilliseconds(50))
            .InvokeCommand(cmd);

        cmd.Subscribe(definitions => HierarchyGridViewModel.Set(definitions));

        return cmd;
    }

    private HierarchyDefinitions DoBuildHierarchyGridDefinitions(
        Seq<TimeSeriesInfo> infos,
        Func<DateOnly, string> formatter,
        Func<double, string> numberFormatter
    )
    {
        var map = infos
            .Where(info => info.Level < Level.Tertiary)
            .Map(info => (info.Identifier, info.Data))
            .ToHashMap();

        var producers = infos
            .Where(info => info.Level < Level.Tertiary)
            .OrderBy(info => info.Identifier)
            .Map(info => new ProducerDefinition
            {
                Content = info.Label,
                Tag = info.Identifier,
                Producer = () => info.Identifier,
            });

        var consumers = infos
            .Where(info => info.Level < Level.Tertiary)
            .GetDates()
            .Order()
            .Map(d => new ConsumerDefinition
            {
                Content = formatter(d),
                Tag = d,
                Consumer = o =>
                    o switch
                    {
                        Identifier identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                x => x,
                                () => double.NaN
                            ),
                        _ => string.Empty,
                    },
                Qualify = o =>
                    o switch
                    {
                        Identifier identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                _ => Qualification.Normal,
                                () => Qualification.Empty
                            ),
                        double dbl
                            => double.IsNaN(dbl) ? Qualification.Empty : Qualification.Normal,
                        _ => Qualification.Unset,
                    },
                Formatter = o =>
                    o switch
                    {
                        double dbl => double.IsNaN(dbl) ? string.Empty : numberFormatter(dbl),
                        _ => string.Empty,
                    },
                ObservableContextItems = o =>
                    o switch
                    {
                        Identifier _ => [.. BuildContext()],
                        _ => [],
                    },
            })
            .ToSeq();

        return new HierarchyDefinitions(producers, consumers);
    }

    private IEnumerable<(
        string description,
        Action<ResultSet> action,
        IObservable<bool> canExecute
    )> BuildContext()
    {
        yield return new(
            "JD+|Disaggregate",
            _ =>
            {
                Signal.Return(SelectedSeries).InvokeCommand(DisaggregateCommand);
            },
            this.WhenAnyValue(x => x.HasConnection)
                .CombineLatest(
                    this.WhenAnyValue(x => x.SelectedSeries)
                        .Select(sel =>
                            sel.HeadOrNone().Match(tsi => tsi.Level == Level.Primary, () => false)
                        )
                )
                .Select(t => t is { First: true, Second: true })
                .ObserveOn(RxSchedulers.MainThreadScheduler)
        );
    }

    private bool DoHoverGrid(Option<(Identifier, DateOnly)> hp)
    {
        if (hp.IsNone)
        {
            HierarchyGridViewModel.HoveredCell = Option<PositionedCell>.None;
            HierarchyGridViewModel.HoveredColumn = -1;
            HierarchyGridViewModel.HoveredRow = -1;

            return false;
        }

        var identifier = hp.Match(t => t.Item1, () => string.Empty);
        var period = hp.Match(t => t.Item2, () => DateOnly.MinValue);

        var cell = HierarchyGridViewModel.DrawnCells.Find(pc =>
            pc.ProducerDefinition.Tag?.Equals(identifier) == true
            && pc.ConsumerDefinition.Tag?.Equals(period) == true
        );

        if (cell.IsNone) /* Cell is not drawn */
        {
            var producerOffset = !HierarchyGridViewModel.DrawnCells.Exists(pc =>
                pc.ProducerDefinition.Tag?.Equals(identifier) == true
            )
                ? HierarchyGridViewModel
                    .Producers.Find(x => x.Tag?.Equals(identifier) == true)
                    .Match(p => p.Position, () => -1)
                : -1;

            var consumerOffset = !HierarchyGridViewModel.DrawnCells.Exists(pc =>
                pc.ConsumerDefinition.Tag?.Equals(period) == true
            )
                ? HierarchyGridViewModel
                    .Consumers.Find(x => x.Tag?.Equals(period) == true)
                    .Match(c => c.Position, () => -1)
                : -1;

            var hOffset = !HierarchyGridViewModel.IsTransposed ? consumerOffset : producerOffset;
            if (hOffset != -1)
                HierarchyGridViewModel.HorizontalOffset = hOffset;

            var vOffset = !HierarchyGridViewModel.IsTransposed ? producerOffset : consumerOffset;
            if (vOffset != -1)
                HierarchyGridViewModel.VerticalOffset = vOffset;

            return hOffset == -1 || vOffset == -1; /* Trigger the method again with elements that should have been drawn */
        }

        HierarchyGridViewModel.HoveredCell = cell;

        HierarchyGridViewModel.HoveredColumn = cell.Match(
            c =>
                !HierarchyGridViewModel.IsTransposed
                    ? c.ConsumerDefinition.Position
                    : c.ProducerDefinition.Position,
            () => -1
        );
        HierarchyGridViewModel.HoveredRow = cell.Match(
            c =>
                !HierarchyGridViewModel.IsTransposed
                    ? c.ProducerDefinition.Position
                    : c.ConsumerDefinition.Position,
            () => -1
        );

        return false;
    }

    public void ClearSelection()
    {
        Selection = Selection.Clear();
    }

    public void Add(TimeSeriesInfo tsi)
    {
        SeriesCache = SeriesCache.AddOrUpdate(
            tsi.Identifier,
            tsi with
            {
                Index = SeriesCache.Count,
                NumberFormat = NumberFormat
            }
        );

        ClearSelection();
    }

    public void Add(IEnumerable<TimeSeriesInfo> series)
    {
        var temp = SeriesCache.AddOrUpdateRange(series.Select(tsi => (tsi.Identifier, tsi)));
        SeriesCache = temp
            .Values.OrderBy(tsi => tsi.Identifier)
            .Map(
                (idx, tsi) =>
                    (tsi.Identifier, tsi with { Index = idx, NumberFormat = NumberFormat })
            )
            .ToHashMap();
        ClearSelection();
    }

    private async Task Clear(bool derivedOnly = false)
    {
        if (derivedOnly)
        {
            var derived = SeriesCache
                .Values.Where(tsi => tsi.IsDerived)
                .Select(tsi => tsi.Identifier)
                .ToSeq();
            var temp = SeriesCache.RemoveRange(derived);
            SeriesCache = temp
                .Values.OrderBy(tsi => tsi.Identifier)
                .Map((idx, tsi) => (tsi.Identifier, tsi with { Index = idx }))
                .ToHashMap();

            await ClearInfoInteraction.Handle(RxVoid.Default);
        }
        else
        {
            SeriesCache = SeriesCache.Clear();
        }

        ClearSelection();
    }

    public void UpdateFormat(string format)
    {
        var updates = SeriesCache
            .Values.Select(s => s with { NumberFormat = format })
            .ToSeq()
            .Map(tsi => (tsi.Identifier, tsi));
        SeriesCache = SeriesCache.AddOrUpdateRange(updates);
    }

    public void RenameSeries(TimeSeriesInfo tsi, string? newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
            return;

        SeriesCache = SeriesCache.AddOrUpdate(tsi.Identifier, tsi with { Label = newName });
        ClearSelection();
    }

    public void Remove(params IEnumerable<Identifier> identifiers)
    {
        var temp = SeriesCache.RemoveRange(identifiers);
        SeriesCache = temp
            .Values.OrderBy(tsi => tsi.Identifier)
            .Map((idx, tsi) => (tsi.Identifier, tsi with { Index = idx }))
            .ToHashMap();
        ClearSelection();
    }

    public void ChangeSeriesChartType(TimeSeriesInfo seriesInfo, SeriesChartType targetType) =>
        ChangeSeriesChartType(seriesInfo.Identifier, targetType);

    public void ChangeSeriesChartType(Identifier id, SeriesChartType targetType)
    {
        SeriesCache = SeriesCache.Find(
            id,
            tsi => SeriesCache.AddOrUpdate(id, tsi with { ChartType = targetType }),
            () => SeriesCache
        );
    }
}
