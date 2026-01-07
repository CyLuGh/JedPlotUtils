using System;
using System.Collections.Frozen;
using System.Reactive;
using System.Reactive.Linq;
using System.Text;
using DynamicData;
using HierarchyGrid.Definitions;
using JedPlotUtils.ScottPlot;
using JedPlotUtils.ScottPlot.Common.Components;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxUnit = System.Reactive.Unit;

namespace JedPlotUtils.Scottplot.Common.Components.ViewModels;

public partial class TimeSeriesViewerViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
    public HierarchyGridViewModel HierarchyGridViewModel { get; } = new();

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }

    private SourceCache<TimeSeriesInfo, string> _infoCache = new(i => i.Identifier);

    public ReactiveCommand<DisplayMode, RxUnit> AdaptDisplayModeCommand { get; }
    public Interaction<DisplayMode, RxUnit> AdaptDisplayModeInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        HierarchyDefinitions
    > BuildHierarchyGridDefinitionsCommand { get; }

    public ReactiveCommand<Seq<TimeSeriesInfo>, RxUnit> DrawChartCommand { get; }
    public Interaction<Seq<PlotSeries>, RxUnit> DrawChartInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public TimeSeriesViewerViewModel()
    {
        AdaptDisplayModeCommand = CreateCommandAdaptDisplayModeCommand();
        BuildHierarchyGridDefinitionsCommand = CreateCommandBuildHierarchyGridDefinitions();
        DrawChartCommand = CreateDrawChartCommand();
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

    private ReactiveCommand<Seq<TimeSeriesInfo>, RxUnit> CreateDrawChartCommand()
    {
        DrawChartInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (Seq<TimeSeriesInfo> seq) => DrawChartInteraction.Handle(seq.Map(x => x.PlotSeries))
        );
        _infoCache.Connect().DisposeMany().Select(_ => _infoCache.Items.ToSeq()).InvokeCommand(cmd);

        return cmd;
    }
}
