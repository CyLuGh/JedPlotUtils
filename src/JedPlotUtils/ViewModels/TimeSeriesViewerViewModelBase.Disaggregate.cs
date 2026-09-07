using JDPlus.WS.Models;
using JedPlotUtils.Models;
using LanguageExt;
using LanguageExt.SomeHelp;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using Splat;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    public ReactiveCommand<
        Option<TimeSeriesInfo>,
        Option<TemporalDisaggregationResults>
    > DisaggregateCommand { get; }

    private ReactiveCommand<
        Option<TimeSeriesInfo>,
        Option<TemporalDisaggregationResults>
    > CreateCommandDisaggregateCommand()
    {
        var canExecute = this.WhenAnyValue(x => x.HasConnection)
            .CombineLatest(this.WhenAnyValue(x => x.SingleSelection).Select(sel => sel.IsSome))
            .Select(t => t is { First: true, Second: true })
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        var cmd = ReactiveCommand.CreateFromTask(
            (Option<TimeSeriesInfo> tsi) => Disaggregate(tsi),
            canExecute
        );

        cmd.ThrownExceptions.Subscribe(ex => this.Log().Error(ex));

        return cmd;
    }

    private async Task<Option<TemporalDisaggregationResults>> Disaggregate(
        Option<TimeSeriesInfo> option
    )
    {
        var elements = from cm in WsManager from tsi in option select (cm, tsi);

        var derived = await elements.MatchAsync(
            async t =>
            {
                var (cm, tsi) = t;
                var data = tsi.Data.ToSeq();
                var tsData = await cm.BuildTsData(
                    data,
                    aggregationType: AggregationType.None,
                    Frequency.Undefined
                );
                var results = await cm.ProcessTemporalDisaggregation(
                    tsData,
                    model: "Rw",
                    algorithm: "SqrtDiffuse",
                    nBackcasts: 0,
                    nForecasts: 6
                );

                return Option<TemporalDisaggregationResults>.Some(results);
            },
            () => Task.FromResult(Option<TemporalDisaggregationResults>.None)
        );

        return derived;
    }

    public ReactiveCommand<
        Option<TemporalDisaggregationResults>,
        RxVoid
    > CreateDisaggregatedSeriesCommand { get; }

    private ReactiveCommand<
        Option<TemporalDisaggregationResults>,
        RxVoid
    > CreateCommandCreateDisaggregatedSeriesCommand(
        ReactiveCommand<
            Option<TimeSeriesInfo>,
            Option<TemporalDisaggregationResults>
        > disaggregateCommand
    )
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            (Option<TemporalDisaggregationResults> o) =>
            {
                Clear(true);
                o.IfSome(res =>
                {
                    var disaggregatedSeries = res.DisaggregatedSeries.GetDateValues();
                    Add(new TimeSeriesInfo(disaggregatedSeries, isDerived: true));

                    var stDevSeries = res.StDevDisaggregatedSeries.GetDateValues();
                    Add(
                        new TimeSeriesInfo(
                            stDevSeries.Select(t =>
                                (t.Key, disaggregatedSeries.Find(t.Key, d => d, () => 0d) + t.Value)
                            ),
                            stDevSeries.Select(t =>
                                (t.Key, disaggregatedSeries.Find(t.Key, d => d, () => 0d) - t.Value)
                            ),
                            isDerived: true
                        )
                    );
                });
            }
        );

        disaggregateCommand
            .Merge(
                disaggregateCommand.ThrownExceptions.Select(_ =>
                    Option<TemporalDisaggregationResults>.None
                )
            )
            .InvokeCommand(cmd);

        return cmd;
    }
}
