using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using JedPlotUtils.ScottPlot;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;

namespace DemoData;

public partial class TimeSeriesSampleViewModel : ReactiveObject
{
    public TimeSeriesSampleViewModel()
    {
        DrawChartCommand = CreateDrawChartCommand(BuildSampleCommand);
    }

    private ReactiveCommand<Seq<ChartTs>, RxVoid> CreateDrawChartCommand(
        ReactiveCommand<RxVoid, Seq<ChartTs>> buildSampleCommand
    )
    {
        DrawChartInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var cmd = ReactiveCommand.CreateFromObservable(
            (Seq<ChartTs> seq) => DrawChartInteraction.Handle(seq.Map(x => x.ToPlotSeries()))
        );
        buildSampleCommand.InvokeCommand(cmd);

        return cmd;
    }

    public Interaction<Seq<PlotSeries>, RxVoid> DrawChartInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    public ReactiveCommand<Seq<ChartTs>, RxVoid> DrawChartCommand { get; }

    [ReactiveCommand]
    private Seq<ChartTs> BuildSample() => new TsGenerator().Sample;
}
