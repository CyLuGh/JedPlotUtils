using System.Collections.ObjectModel;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using DynamicData;
using HierarchyGrid.Definitions;
using JedPlotUtils.Models;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using Unit = System.Reactive.Unit;

namespace JedPlotUtils.ViewModels;

public abstract partial class TimeSeriesViewerViewModelBase : BaseViewModel
{
    protected readonly SourceCache<TimeSeriesInfo, Identifier> _seriesCache = new(x =>
        x.Identifier
    );
    protected readonly IObservable<IChangeSet<TimeSeriesInfo, Identifier>> _cacheUpdates;

    protected readonly ReadOnlyObservableCollection<TimeSeriesInfo> _seriesInfos;
    public ReadOnlyObservableCollection<TimeSeriesInfo> SeriesInfos => _seriesInfos;

    public HierarchyGridViewModel HierarchyGridViewModel { get; } = new();

    [Reactive]
    public partial Option<Func<DateOnly, string>> DateFormatter { get; set; }

    [Reactive]
    public partial Option<(Identifier, DateOnly)> HoveredPoint { get; set; }

    public ReactiveCommand<Option<(Identifier, DateOnly)>, bool> HighlightCellGridCommand { get; }
    public ReactiveCommand<(Identifier, DateOnly), Unit> HighlightChartPointCommand { get; }

    public ReactiveCommand<
        (Seq<TimeSeriesInfo>, Func<DateOnly, string>),
        HierarchyDefinitions
    > BuildHierarchyGridDefinitions { get; }

    public Interaction<(Identifier, DateOnly), Unit> HighlightChartPointInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }
    public ReactiveCommand<DisplayMode, Unit> AdaptDisplayModeCommand { get; }
    public Interaction<DisplayMode, Unit> AdaptDisplayModeInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    protected TimeSeriesViewerViewModelBase()
    {
        _cacheUpdates = _seriesCache.Connect().RefCount();

        _cacheUpdates
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out _seriesInfos)
            .DisposeMany()
            .Subscribe();

        AdaptDisplayModeCommand = CreateCommandAdaptDisplayModeCommand();
        HighlightCellGridCommand = CreateCommandHighlightGridCommand();
        HighlightChartPointCommand = CreateHighlightPointCommand();
        BuildHierarchyGridDefinitions = CreateCommandBuildHierarchyGridDefinitions();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.HoveredPoint)
                .Select(o => o.Match(Observable.Return, Observable.Empty<(Identifier, DateOnly)>))
                .Switch()
                .InvokeCommand(HighlightChartPointCommand)
                .DisposeWith(disposables);
        });
    }

    private ReactiveCommand<
        Option<(Identifier, DateOnly)>,
        bool
    > CreateCommandHighlightGridCommand()
    {
        var cmd = ReactiveCommand.Create(
            (Option<(Identifier, DateOnly)> si) => DoHighlightGrid(si)
        );

        this.WhenAnyValue(x => x.HoveredPoint)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .InvokeCommand(cmd);

        this.WhenAnyValue(x => x.HoveredPoint)
            .DistinctUntilChanged()
            .CombineLatest(cmd.Where(x => x == true))
            .Select(t => t.First)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .InvokeCommand(cmd);

        cmd.Where(x => !x).InvokeCommand(HierarchyGridViewModel, x => x.DrawGridCommand);

        return cmd;
    }

    private ReactiveCommand<DisplayMode, Unit> CreateCommandAdaptDisplayModeCommand()
    {
        AdaptDisplayModeInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (DisplayMode dm) => AdaptDisplayModeInteraction.Handle(dm)
        );
        this.WhenAnyValue(x => x.DisplayMode).InvokeCommand(cmd);
        return cmd;
    }

    private ReactiveCommand<(Identifier, DateOnly), Unit> CreateHighlightPointCommand()
    {
        HighlightChartPointInteraction.RegisterHandler(ctx => ctx.SetOutput(Unit.Default));
        var cmd = ReactiveCommand.CreateFromObservable<(Identifier, DateOnly), Unit>(t =>
            HighlightChartPointInteraction.Handle(t)
        );
        return cmd;
    }

    private ReactiveCommand<
        (Seq<TimeSeriesInfo>, Func<DateOnly, string>),
        HierarchyDefinitions
    > CreateCommandBuildHierarchyGridDefinitions()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            ((Seq<TimeSeriesInfo>, Func<DateOnly, string>) t) =>
            {
                var (infos, formatter) = t;
                return DoBuildHierarchyGridDefinitions(infos, formatter);
            }
        );

        _cacheUpdates
            .DisposeMany()
            .Select(_ => _seriesCache.Items.ToSeq())
            .CombineLatest(
                this.WhenAnyValue(x => x.DateFormatter)
                    .Select(o => o.Match(f => f, () => d => d.ToString("yyyy-MM")))
            )
            .InvokeCommand(cmd);

        cmd.Subscribe(defs => HierarchyGridViewModel.Set(defs));

        return cmd;
    }

    private static HierarchyDefinitions DoBuildHierarchyGridDefinitions(
        Seq<TimeSeriesInfo> infos,
        Func<DateOnly, string> formatter
    )
    {
        var map = infos.Map(info => (info.Identifier, info.Data)).ToHashMap();

        var producers = infos.Map(info => new ProducerDefinition
        {
            Content = info.Label,
            Tag = info.Identifier,
            Producer = () => info.Identifier,
        });

        var consumers = infos
            .GetDates()
            .Order()
            .Map(d => new ConsumerDefinition
            {
                Content = formatter(d),
                Tag = d,
                Consumer = o =>
                    o switch
                    {
                        Identifier identifier => (
                            from s in map.Find(identifier)
                            from v in s.Find(d)
                            select v
                        ).Match(x => x, () => double.NaN),
                        _ => string.Empty,
                    },
                Qualify = o =>
                    o switch
                    {
                        Identifier identifier => (
                            from s in map.Find(identifier)
                            from v in s.Find(d)
                            select v
                        ).Match(_ => Qualification.Normal, () => Qualification.Empty),
                        double d => double.IsNaN(d) ? Qualification.Empty : Qualification.Normal,
                        _ => Qualification.Unset,
                    },
                Formatter = o =>
                    o switch
                    {
                        double d => double.IsNaN(d) ? string.Empty : d.ToString(),
                        _ => string.Empty,
                    },
            })
            .ToSeq();

        return new HierarchyDefinitions(producers, consumers);
    }

    private bool DoHighlightGrid(Option<(Identifier, DateOnly)> hp)
    {
        if (hp.IsNone)
        {
            HierarchyGridViewModel.HoveredCell = Option<PositionedCell>.None;
            return false;
        }

        var identifier = hp.Match(t => t.Item1, () => string.Empty);
        var period = hp.Match(t => t.Item2, () => DateOnly.MinValue);

        var cell = HierarchyGridViewModel.DrawnCells.Find(pc =>
            pc.ProducerDefinition.Tag?.Equals(identifier) == true
            && period.Equals(((DateOnly)pc.ConsumerDefinition.Tag))
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
                period.Equals(((DateOnly)pc.ConsumerDefinition.Tag))
            )
                ? HierarchyGridViewModel
                    .Consumers.Find(x => period.Equals(((DateOnly)x.Tag)))
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

    public void Add(TimeSeriesInfo tsi)
    {
        _seriesCache.AddOrUpdate(tsi);
    }

    public void Clear()
    {
        _seriesCache.Clear();
    }
}
