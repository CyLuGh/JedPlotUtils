using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Text;
using System.Threading.Tasks;
using JedPlotUtils.ScottPlot;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxUnit = System.Reactive.Unit;

namespace DemoData;

public partial class TimeSeriesSampleViewModel : ReactiveObject
{
    public TimeSeriesSampleViewModel()
    {
        DrawChartCommand = CreateDrawChartCommand(BuildSampleCommand);
    }

    private ReactiveCommand<Seq<ChartTs>, RxUnit> CreateDrawChartCommand(
        ReactiveCommand<RxUnit, Seq<ChartTs>> buildSampleCommand
    )
    {
        DrawChartInteraction.RegisterHandler(ctx => ctx.SetOutput(RxUnit.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (Seq<ChartTs> seq) => DrawChartInteraction.Handle(seq.Map(x => x.ToPlotSeries()))
        );
        buildSampleCommand.InvokeCommand(cmd);

        return cmd;
    }

    public Interaction<Seq<PlotSeries>, RxUnit> DrawChartInteraction { get; } =
        new(RxApp.MainThreadScheduler);

    public ReactiveCommand<Seq<ChartTs>, RxUnit> DrawChartCommand { get; }

    [ReactiveCommand]
    private Seq<ChartTs> BuildSample() => new TsGenerator().Sample;
}
