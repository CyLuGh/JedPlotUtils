global using ReactiveUI.Binding;
global using RxCommand = ReactiveUI.ReactiveCommand<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;
global using RxInteraction = ReactiveUI.Binding.Interaction<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;
using System.Globalization;
using System.Text;
using HierarchyGrid.Definitions;
using JDPlus.WS.Client;
using JDPlus.WS.Models;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using LanguageExt.Common;
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
    public partial HashMap<Identifier, TimeSeriesInfo> SeriesCache { get; protected set; }

    public HierarchyGridViewModel HierarchyGridViewModel { get; } =
        new() { StatusMessage = "No data" };

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
    public partial Option<(Identifier, DateOnly)> ThrottledHoveredPoint { get; }

    /// <summary>
    /// Prevents flickering on the first render.
    /// </summary>
    [Reactive]
    public partial bool HasFirstRender { get; set; }

    public ReactiveCommand<Option<(Identifier, DateOnly)>, bool> HoverCellGridCommand { get; }
    public ReactiveCommand<
        Option<(Identifier, DateOnly)>,
        RxVoid
    > HighlightChartPointCommand { get; }

    public ReactiveCommand<
        (
            Seq<TimeSeriesInfo>,
            HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>,
            Func<DateOnly, string>,
            Func<double, string>
        ),
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

    [ObservableAsProperty]
    public partial Option<CommunicationManager> WsManager { get; }

    [ObservableAsProperty]
    public partial bool IsConnecting { get; }

    [ObservableAsProperty]
    public partial bool HasConnection { get; }

    public ReactiveCommand<string?, Option<CommunicationManager>> GetConnectionCommand { get; }

    public ReactiveCommand<LanguageExt.HashSet<Identifier>, RxVoid> ToggleHighlightsCommand { get; }

    [ObservableAsProperty]
    public partial Seq<TimeSeriesInfo> SelectedSeries { get; }

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
    public ReactiveCommand<LanguageExt.HashSet<Identifier>, RxVoid> RemoveUnselectedCommand { get; }
    public RxCommand ClearSeriesCommand { get; }

    public RxCommand CopyToClipboardCommand { get; }
    public Interaction<Seq<TimeSeriesInfo>, RxVoid> CopyToClipboardInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public RxCommand PasteFromClipboardCommand { get; }
    public Interaction<RxVoid, Seq<TimeSeriesInfo>> PasteFromClipboardInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    public RxInteraction ClearInfoInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    protected RxCommand ClearResultsCommand { get; }

    public RxCommand CancelCommand { get; }

    [ObservableAsProperty]
    public partial bool IsWorking { get; }

    protected TimeSeriesViewerViewModelBase()
    {
        ClearInfoInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        NumberFormat = "F0";
        Configuration = TimeSeriesViewerSettings.Default;

        CancelCommand = ReactiveCommand.Create(() => { });

        ShowSettingsCommand = CreateCommandShowSettingsCommand();
        ShowConnectionSettingsCommand = CreateCommandShowConnectionSettingsCommand();
        ClearDerivedCommand = ReactiveCommand.CreateFromTask(async () => await Clear(true));
        ClearSeriesCommand = ReactiveCommand.CreateFromTask(async () => await Clear());
        ClearResultsCommand = ReactiveCommand.Create(() => { });

        AdaptDisplayModeCommand = CreateCommandAdaptDisplayModeCommand();
        HoverCellGridCommand = CreateCommandHoverGridCommand();
        HighlightChartPointCommand = CreateHighlightPointCommand();
        BuildHierarchyGridDefinitions = CreateCommandBuildHierarchyGridDefinitions();
        AdaptXAxisCommand = CreateCommandAdaptXAxisCommand();

        GetConnectionCommand = CreateCommandGetConnection(ShowConnectionSettingsCommand);
        ToggleHighlightsCommand = CreateCommandToggleHighlights();

        DisaggregateCommand = CreateCommandDisaggregateCommand(
            ClearSeriesCommand,
            ClearDerivedCommand,
            CancelCommand
        );
        CreateDisaggregatedSeriesCommand = CreateCommandCreateDisaggregatedSeriesCommand();
        ChangeFrequencyCommand = CreateCommandChangeFrequencyCommand();

        TramoForecastCommand = CreateCommandTramoForecast(CancelCommand);
        CreateTramoForecastsSeriesCommand = CreateCommandCreateTramoForecastsSeriesCommand();
        TramoFullProcessCommand = CreateCommandTramoFullProcessCommand(CancelCommand);
        CreateTramoOutputSeriesCommand = CreateCommandCreateTramoOutputSeriesCommand();
        BuildOutliersMap = CreateCommandBuildOutliersMap();

        RenameSeriesCommand = CreateCommandRenameSeriesCommand();
        RemoveSelectionCommand = CreateCommandRemoveSelectionCommand();
        RemoveUnselectedCommand = CreateCommandRemoveSelectionCommand(true);

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
                .DistinctUntilChanged() // Make sure we only update the settings when they change
                .ObserveOn(RxSchedulers.MainThreadScheduler)
                .Subscribe(settings =>
                {
                    Signal.Return(settings.WebServiceAddress).InvokeCommand(GetConnectionCommand);
                    Configuration = settings;
                    HierarchyGridViewModel.Theme = new LightGridTheme();
                })
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.Configuration)
                .Throttle(TimeSpan.FromMilliseconds(200))
                .ObserveOn(RxSchedulers.MainThreadScheduler)
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
                .SwitchTo()
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
                .SwitchTo()
                .Subscribe(x =>
                {
                    Selection = Selection.Remove(x);
                });

            this.WhenAnyValue(x => x.NumberFormat).Subscribe(UpdateFormat).DisposeWith(disposables);

            this.WhenAnyValue(x => x.DisplayMode)
                .Select(mode => AdaptDisplayModeCommand.Execute(mode))
                .SwitchTo()
                .Subscribe()
                .DisposeWith(disposables);

            _isWorkingHelper = TramoForecastCommand
                .IsExecuting.CombineLatest(
                    TramoFullProcessCommand.IsExecuting,
                    DisaggregateCommand.IsExecuting,
                    BuildOutliersMap.IsExecuting
                )
                .Select(t => t.First || t.Second || t.Third || t.Fourth)
                .ToProperty(this, x => x.IsWorking, scheduler: RxSchedulers.MainThreadScheduler)
                .DisposeWith(disposables);
        });
    }

    private ReactiveCommand<
        (HashMap<Identifier, Either<Error, TramoOutput>>, HashMap<Identifier, TimeSeriesInfo>),
        HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>
    > CreateCommandBuildOutliersMap()
    {
        var cmd = ReactiveCommand.CreateRunInBackground<
            (HashMap<Identifier, Either<Error, TramoOutput>>, HashMap<Identifier, TimeSeriesInfo>),
            HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>
        >(tuple =>
        {
            var (map, seriesCache) = tuple;
            return map.SelectMany(t =>
                    t.Value.Match(
                        x =>
                            x.GetOutliers()
                                .Select<(
                                    (Identifier, DateOnly),
                                    Either<Error, TramoOutput.OutlierReport>
                                )>(o => ((t.Key, o.Period), o))
                                .ToSeq(),
                        error =>
                            seriesCache.Find(
                                t.Key,
                                s =>
                                    s.Data.Keys.Map<
                                            DateOnly,
                                            (
                                                (Identifier, DateOnly),
                                                Either<Error, TramoOutput.OutlierReport>
                                            )
                                        >(d => ((t.Key, d), error))
                                        .ToSeq(),
                                () =>
                                    Seq<(
                                        (Identifier, DateOnly),
                                        Either<Error, TramoOutput.OutlierReport>
                                    )>.Empty
                            )
                    )
                )
                .ToHashMap();
        });

        this.WhenAnyValue(x => x.TramoOutput)
            .CombineLatest(this.WhenAnyValue(x => x.SeriesCache))
            .InvokeCommand(cmd);

        _tramoOutliersHelper = cmd.ToProperty(
            this,
            x => x.TramoOutliers,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        return cmd;
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
        cmd.ThrownExceptions.Subscribe(ex =>
            this.Log().Error("Failed to paste from clipboard", ex)
        );
        return cmd;
    }

    private RxCommand CreateCommandCopyToClipboardCommand()
    {
        CopyToClipboardInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var canExecute = this.WhenAnyValue(x => x.SelectedSeries)
            .Select(sel => !sel.IsEmpty)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.CreateFromTask(
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
        /* Register default handler */
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
    > CreateCommandRemoveSelectionCommand(bool keepSelected = false)
    {
        var canExecute = this.WhenAnyValue(x => x.Selection)
            .Select(sel => !sel.IsEmpty)
            .ObserveOn(RxSchedulers.MainThreadScheduler);
        var cmd = ReactiveCommand.Create(
            (LanguageExt.HashSet<Identifier> selection) =>
            {
                if (keepSelected)
                {
                    Remove(SeriesCache.Keys.Except(selection));
                }
                else
                {
                    Remove(selection);
                }
            },
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
                cmd.ThrownExceptions.Select(exc =>
                {
#if DEBUG
                    Console.WriteLine(exc);
#endif
                    return Option<CommunicationManager>.None;
                })
            )
            .ToProperty(this, x => x.WsManager, initialValue: Option<CommunicationManager>.None);

        return cmd;
    }

    private ReactiveCommand<Func<DateOnly, string>, Unit> CreateCommandAdaptXAxisCommand()
    {
        AdaptXAxisInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
        var cmd = ReactiveCommand.CreateFromTask<Func<DateOnly, string>, Unit>(f =>
            AdaptXAxisInteraction.Handle(f)
        );

        this.WhenAnyValue(x => x.DateFormatter)
            .Select(o =>
                o.Match(
                    Signal.Return,
                    () => Signal.Return<Func<DateOnly, string>>(d => d.ToString("yyyy-MM"))
                )
            )
            .SwitchTo()
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
        AdaptDisplayModeInteraction.RegisterHandler(ctx =>
        {
#if DEBUG
            Console.WriteLine($"Calling _DEFAULT_ AdaptDisplayModeInteraction with {ctx.Input}");
#endif
            ctx.SetOutput(RxVoid.Default);
        });

        var cmd = ReactiveCommand.CreateFromTask(
            (DisplayMode dm) => AdaptDisplayModeInteraction.Handle(dm)
        );

        return cmd;
    }

    private ReactiveCommand<Option<(Identifier, DateOnly)>, RxVoid> CreateHighlightPointCommand()
    {
        HighlightChartPointInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var cmd = ReactiveCommand.CreateFromTask<Option<(Identifier, DateOnly)>, RxVoid>(t =>
            HighlightChartPointInteraction.Handle(t)
        );

        this.WhenAnyValue(x => x.ThrottledHoveredPoint)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(20))
            .InvokeCommand(cmd);

        return cmd;
    }

    private ReactiveCommand<
        (
            Seq<TimeSeriesInfo>,
            HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>,
            Func<DateOnly, string>,
            Func<double, string>
        ),
        HierarchyDefinitions
    > CreateCommandBuildHierarchyGridDefinitions()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            (
                (
                    Seq<TimeSeriesInfo>,
                    HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>,
                    Func<DateOnly, string>,
                    Func<double, string>
                ) t
            ) =>
            {
                var (infos, tramoOutliers, dateFormatter, numberFormatter) = t;
                return DoBuildHierarchyGridDefinitions(
                    infos,
                    tramoOutliers,
                    dateFormatter,
                    numberFormatter
                );
            }
        );

        this.WhenAnyValue(x => x.SeriesCache)
            .Select(hm => hm.Values.Where(info => info.Level < Level.Tertiary).ToSeq())
            .DistinctUntilChanged()
            .CombineLatest(
                this.WhenAnyValue(x => x.TramoOutliers),
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
        HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>> tramoOutliers,
        Func<DateOnly, string> formatter,
        Func<double, string> numberFormatter
    )
    {
        var map = infos.Map(info => (info.Identifier, info.Data)).ToHashMap();
        // TODO: improve
        var outliersHighlight = new OutliersHighlight("Bank".ToPalette());

        var producers = infos
            .Where(info => info.Level < Level.Tertiary)
            .OrderBy(info => info.Level)
            .ThenBy(info => info.Index)
            .Map(info => new ProducerDefinition
            {
                Content = info.Label,
                Tooltip = info.Label,
                Tag = info.Identifier,
                Producer = () => info.Identifier,
            })
            .ToSeq();

        var consumers = infos
            .Where(info => info.Level < Level.Tertiary)
            .GetDatesFrozenSet()
            .Order()
            .Map(d => new ConsumerDefinition
            {
                Content = formatter(d),
                Tooltip = formatter(d),
                Tag = d,
                Consumer = o =>
                    o switch
                    {
                        Identifier identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                x =>
                                    Option<(
                                        double,
                                        Option<Either<Error, TramoOutput.OutlierReport>>
                                    )>.Some((x, tramoOutliers.Find((identifier, d)))),
                                () =>
                                    Option<(
                                        double,
                                        Option<Either<Error, TramoOutput.OutlierReport>>
                                    )>.None
                            ),
                        _ => string.Empty,
                    },
                Qualify = o =>
                    o switch
                    {
                        Identifier identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                _ =>
                                    tramoOutliers.Find(
                                        (identifier, d),
                                        _ => Qualification.Error,
                                        () => Qualification.Normal
                                    ),
                                () => Qualification.Empty
                            ),
                        Option<(double, Option<Either<Error, TramoOutput.OutlierReport>>)> option
                            => option.Match(
                                t =>
                                    t.Item2.Match(
                                        either =>
                                            either.Match(
                                                _ => Qualification.Custom, /* With an outlier report */
                                                _ => Qualification.Warning /* With an error */
                                            ),
                                        () => Qualification.Normal
                                    ),
                                () => Qualification.Empty
                            ),
                        double dbl
                            => double.IsNaN(dbl) ? Qualification.Empty : Qualification.Normal,
                        _ => Qualification.Unset,
                    },
                Colorize = o =>
                    o switch
                    {
                        Option<(double, Option<Either<Error, TramoOutput.OutlierReport>>)> option
                            => option.Match(
                                t =>
                                    t.Item2.Match(
                                        either =>
                                            (
                                                outliersHighlight
                                                    .GetOutlierColor(
                                                        either.Match(r => r.Code, _ => string.Empty)
                                                    )
                                                    .ToThemeColor(),
                                                outliersHighlight
                                                    .GetOutlierContrastColor(
                                                        either.Match(r => r.Code, _ => string.Empty)
                                                    )
                                                    .ToThemeColor()
                                            ),
                                        () => (ThemeColors.White, ThemeColors.Black)
                                    ),
                                () => (ThemeColors.White, ThemeColors.Black)
                            ),
                        _ => (ThemeColors.White, ThemeColors.Black)
                    },
                Formatter = o =>
                    o switch
                    {
                        Option<(double, Option<Either<Error, TramoOutput.OutlierReport>>)> option
                            => option.Match(
                                t =>
                                    double.IsNaN(t.Item1) ? string.Empty : numberFormatter(t.Item1),
                                () => string.Empty
                            ),
                        double dbl => double.IsNaN(dbl) ? string.Empty : numberFormatter(dbl),
                        _ => string.Empty,
                    },
                ContextItems = _ => [.. BuildStaticContext()],
                ObservableContextItems = o =>
                    o switch
                    {
                        Identifier _ => [.. BuildContext()],
                        _ => [],
                    },
                TooltipCreator = (_, c) =>
                    c switch
                    {
                        Option<(double, Option<Either<Error, TramoOutput.OutlierReport>>)> option
                            => option.Match(
                                t => t.Item2.Match(BuildTooltip, () => string.Empty),
                                () => string.Empty
                            ),
                        _ => string.Empty,
                    }
            })
            .ToSeq();

        return new HierarchyDefinitions(producers, consumers);

        string BuildTooltip(Either<Error, TramoOutput.OutlierReport> outlier)
        {
            StringBuilder sb = new();

            outlier.IfRight(o =>
            {
                sb.AppendLine($"Outlier type: {o.Code}");
                sb.Append($"Outlier value: {numberFormatter(o.Coefficient.Value)}");
            });

            outlier.IfLeft(error =>
            {
                sb.AppendLine("Outliers couldn't be calculated");
                sb.AppendLine(error.Message);
            });

            return sb.ToString();
        }
    }

    private IEnumerable<(string description, Action<ResultSet> action)> BuildStaticContext()
    {
        yield return new(
            "Settings",
            _ =>
            {
                Signal.Return(Configuration).InvokeCommand(ShowSettingsCommand);
            }
        );
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
                NumberFormat = NumberFormat,
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

    public async Task Clear(bool derivedOnly = false)
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

        Signal.Return(RxVoid.Default).InvokeCommand(ClearResultsCommand);
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
