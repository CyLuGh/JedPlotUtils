using System;
using System.Collections.Frozen;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using DynamicData;
using HierarchyGrid.Definitions;
using JedPlotUtils.ScottPlot;
using JedPlotUtils.ScottPlot.Common.Components;
using LanguageExt;
using LanguageExt.UnsafeValueAccess;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using ScottPlot;
using ScottPlot.Plottables;
using ScottPlot.Statistics;
using RxUnit = System.Reactive.Unit;

namespace JedPlotUtils.Scottplot.Common.Components.ViewModels;

public partial class TimeSeriesViewerViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public HierarchyGridViewModel HierarchyGridViewModel { get; } = new();

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }

    [Reactive]
    public partial Option<(Scatter, DateOnly)> HoveredPoint { get; set; }

    [ObservableAsProperty(ReadOnly = false)]
    private HashMap<Scatter, string> _series;

    private readonly SourceCache<TimeSeriesInfo, string> _infoCache = new(i => i.Identifier);

    public ReactiveCommand<DisplayMode, RxUnit> AdaptDisplayModeCommand { get; }
    public Interaction<DisplayMode, RxUnit> AdaptDisplayModeInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        HierarchyDefinitions
    > BuildHierarchyGridDefinitionsCommand { get; }

    public ReactiveCommand<Seq<TimeSeriesInfo>, HashMap<Scatter, string>> DrawChartCommand { get; }
    public Interaction<Seq<PlotSeries>, HashMap<Scatter, string>> DrawChartInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<Option<(Scatter, DateOnly)>, bool> HighlightGridCommand { get; }
    public ReactiveCommand<Option<(Scatter, DateOnly)>, RxUnit> HighlightChartCommand { get; }
    public Interaction<Option<(Scatter, DateOnly)>, RxUnit> HighlightChartInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public TimeSeriesViewerViewModel()
    {
        AdaptDisplayModeCommand = CreateCommandAdaptDisplayModeCommand();
        BuildHierarchyGridDefinitionsCommand = CreateCommandBuildHierarchyGridDefinitions();
        DrawChartCommand = CreateDrawChartCommand();
        HighlightGridCommand = CreateCommandHighlightGridCommand();
        HighlightChartCommand = CreateCommandHighlightChartCommand();

        HierarchyGridViewModel
            .WhenAnyValue(x => x.HoveredCell)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .DistinctUntilChanged()
            .Subscribe(o =>
            {
                HoveredPoint = o.Match(
                    pc =>
                    {
                        var period = (DateOnly)pc.ConsumerDefinition.Tag;
                        var identifier = (string)pc.ProducerDefinition.Tag;

                        return Series
                            .Find(x => x.Value.Equals(identifier))
                            .Match(t => (t.Key, period), () => Option<(Scatter, DateOnly)>.None);
                    },
                    () => Option<(Scatter, DateOnly)>.None
                );
            });
    }

    private ReactiveCommand<
        Option<(Scatter, DateOnly)>,
        RxUnit
    > CreateCommandHighlightChartCommand()
    {
        HighlightChartInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (Option<(Scatter, DateOnly)> si) => HighlightChartInteraction.Handle(si)
        );

        this.WhenAnyValue(x => x.HoveredPoint)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .InvokeCommand(cmd);

        return cmd;
    }

    private ReactiveCommand<Option<(Scatter, DateOnly)>, bool> CreateCommandHighlightGridCommand()
    {
        var cmd = ReactiveCommand.Create((Option<(Scatter, DateOnly)> si) => DoHighlightGrid(si));

        this.WhenAnyValue(x => x.HoveredPoint)
            .DistinctUntilChanged()
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(cmd);

        this.WhenAnyValue(x => x.HoveredPoint)
            .DistinctUntilChanged()
            .CombineLatest(cmd.Where(x => x == true))
            .Select(t => t.First)
            .Throttle(TimeSpan.FromMilliseconds(50))
            .ObserveOn(RxApp.MainThreadScheduler)
            .InvokeCommand(cmd);

        cmd.Where(x => x == false).InvokeCommand(HierarchyGridViewModel, x => x.DrawGridCommand);

        return cmd;
    }

    private bool DoHighlightGrid(Option<(Scatter, DateOnly)> hp)
    {
        if (hp.IsNone)
        {
            HierarchyGridViewModel.HoveredCell = Option<PositionedCell>.None;
            return false;
        }

        var identifier = (from t in hp from id in Series.Find(t.Item1) select id).Match(
            x => x,
            () => string.Empty
        );
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

    private ReactiveCommand<DisplayMode, RxUnit> CreateCommandAdaptDisplayModeCommand()
    {
        AdaptDisplayModeInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));

        var cmd = ReactiveCommand.CreateFromObservable(
            (DisplayMode dm) => AdaptDisplayModeInteraction.Handle(dm)
        );

        this.WhenAnyValue(x => x.DisplayMode).InvokeCommand(cmd);

        return cmd;
    }

    public void AddTimeSeriesInfo(TimeSeriesInfo info)
    {
        _infoCache.AddOrUpdate(info);
    }

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        HierarchyDefinitions
    > CreateCommandBuildHierarchyGridDefinitions()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            (Seq<TimeSeriesInfo> infos) => DoBuildHierarchyGridDefinitions(infos)
        );

        _infoCache.Connect().DisposeMany().Select(_ => _infoCache.Items.ToSeq()).InvokeCommand(cmd);
        cmd.Subscribe(defs => HierarchyGridViewModel.Set(defs));

        return cmd;
    }

    private static HierarchyDefinitions DoBuildHierarchyGridDefinitions(Seq<TimeSeriesInfo> infos)
    {
        var map = infos.Map(info => (info.Identifier, info.Data)).ToHashMap();

        var producers = infos.Map(info => new ProducerDefinition
        {
            Content = info.Label,
            Tag = info.Identifier,
            Producer = () => info.Identifier
        });

        var consumers = infos
            .GetDates()
            .Order()
            .Map(d => new ConsumerDefinition
            {
                Content = d,
                Tag = d,
                Consumer = o =>
                    o switch
                    {
                        string identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                x => x,
                                () => double.NaN
                            ),
                        _ => string.Empty
                    },
                Qualify = o =>
                    o switch
                    {
                        string identifier
                            => (from s in map.Find(identifier) from v in s.Find(d) select v).Match(
                                _ => Qualification.Normal,
                                () => Qualification.Empty
                            ),
                        double d => double.IsNaN(d) ? Qualification.Empty : Qualification.Normal,
                        _ => Qualification.Unset
                    },
                Formatter = o =>
                    o switch
                    {
                        double d => double.IsNaN(d) ? string.Empty : d.ToString(),
                        _ => string.Empty
                    }
            })
            .ToSeq();

        return new HierarchyDefinitions(producers, consumers);
    }

    private ReactiveCommand<Seq<TimeSeriesInfo>, HashMap<Scatter, string>> CreateDrawChartCommand()
    {
        DrawChartInteraction.RegisterHandler(ctx => ctx.SetOutput(HashMap<Scatter, string>.Empty));

        var cmd = ReactiveCommand.CreateFromObservable(
            (Seq<TimeSeriesInfo> seq) => DrawChartInteraction.Handle(seq.Map(x => x.PlotSeries))
        );

        _seriesHelper = cmd.ToProperty(this, x => x.Series, scheduler: RxApp.MainThreadScheduler);

        _infoCache.Connect().DisposeMany().Select(_ => _infoCache.Items.ToSeq()).InvokeCommand(cmd);

        return cmd;
    }
}
