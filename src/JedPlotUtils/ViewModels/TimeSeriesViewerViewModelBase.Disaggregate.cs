using JDPlus.WS.Models;
using JedPlotUtils.Models;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using ReactiveUI.SourceGenerators;
using Splat;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    private bool _hasDisaggregationResults;

    [ObservableAsProperty(ReadOnly = false)]
    private Option<TemporalDisaggregationResults> _disaggregationResults;

    private ReactiveCommand<
        Option<TemporalDisaggregationResults>,
        RxVoid
    > ShowDisaggregationInfo { get; }

    public Interaction<
        TemporalDisaggregationResults,
        RxVoid
    > ShowDisaggregationInfoInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    private ReactiveCommand<
        Option<TemporalDisaggregationResults>,
        RxVoid
    > CreateCommandShowDisaggregationInfo()
    {
        ShowDisaggregationInfoInteraction.RegisterHandler(ctx => ctx.SetOutput(RxVoid.Default));
        var cmd = ReactiveCommand.CreateFromTask(
            async (Option<TemporalDisaggregationResults> o) =>
            {
                await o.IfSomeAsync(async r => await ShowDisaggregationInfoInteraction.Handle(r));
            }
        );

        this.WhenAnyValue(x => x.DisaggregationResults).InvokeCommand(cmd);

        return cmd;
    }

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TemporalDisaggregationResults>
    > DisaggregateCommand { get; }

    public Interaction<
        RxVoid,
        Option<TemporalDisaggregationRequest>
    > GetDisaggregationRequestInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TemporalDisaggregationResults>
    > CreateCommandDisaggregateCommand(RxCommand clearSeriesCommand, RxCommand clearDerivedCommand)
    {
        GetDisaggregationRequestInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(
                new TemporalDisaggregationRequest()
                {
                    Model = "Rw",
                    Algorithm = "SqrtDiffuse",
                    NBackcasts = 0,
                    NForecasts = 6
                }
            )
        );

        var canExecute = this.WhenAnyValue(x => x.HasConnection)
            .CombineLatest(
                this.WhenAnyValue(x => x.SelectedSeries)
                    .Select(sel =>
                        sel.HeadOrNone().Match(tsi => tsi.Level == Level.Primary, () => false)
                    )
            )
            .Select(t => t is { First: true, Second: true })
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        var cmd = ReactiveCommand.CreateFromTask(
            async (Seq<TimeSeriesInfo> seq) =>
            {
                var req = await GetDisaggregationRequestInteraction.Handle(RxVoid.Default);
                return await Disaggregate(seq.HeadOrNone(), req).ConfigureAwait(false);
            },
            canExecute
        );

        _disaggregationResultsHelper = Signal
            .Merge(
                cmd,
                cmd.ThrownExceptions.Select(_ => Option<TemporalDisaggregationResults>.None),
                clearSeriesCommand.Select(_ => Option<TemporalDisaggregationResults>.None),
                clearDerivedCommand.Select(_ => Option<TemporalDisaggregationResults>.None)
            )
            .ToProperty(
                this,
                x => x.DisaggregationResults,
                scheduler: RxSchedulers.MainThreadScheduler
            );

        cmd.ThrownExceptions.Subscribe(ex => this.Log().Error(ex));

        return cmd;
    }

    private async Task<Option<TemporalDisaggregationResults>> Disaggregate(
        Option<TimeSeriesInfo> oSeries,
        Option<TemporalDisaggregationRequest> oRequest
    )
    {
        var elements =
            from cm in WsManager
            from tsi in oSeries
            from req in oRequest
            select (cm, tsi, req);

        var derived = await elements.MatchAsync(
            async t =>
            {
                var (cm, tsi, req) = t;
                var data = tsi.Data.ToSeq();

                var tsData = await cm.BuildTsData(
                    data,
                    aggregationType: AggregationType.None,
                    Frequency.Undefined
                );

                var results = await cm.ProcessTemporalDisaggregation(req with { Y = tsData });

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
    > CreateCommandCreateDisaggregatedSeriesCommand()
    {
        var cmd = ReactiveCommand.CreateFromTask(
            async (Option<TemporalDisaggregationResults> o) =>
            {
                await Clear(true).ConfigureAwait(false);
                o.IfSome(res =>
                {
                    var disaggregatedSeries = res.DisaggregatedSeries.GetDateValues();
                    var stDevSeries = res.StDevDisaggregatedSeries.GetDateValues();

                    Add(
                        new TimeSeriesInfo(
                            stDevSeries.Select(t =>
                                (t.Key, disaggregatedSeries.Find(t.Key, d => d, () => 0d) + t.Value)
                            ),
                            stDevSeries.Select(t =>
                                (t.Key, disaggregatedSeries.Find(t.Key, d => d, () => 0d) - t.Value)
                            ),
                            level: Level.Tertiary
                        )
                    );

                    Add(
                        new TimeSeriesInfo(
                            "Disaggregated",
                            disaggregatedSeries,
                            level: Level.Secondary
                        )
                    );
                });
            }
        );

        this.WhenAnyValue(x => x.DisaggregationResults).InvokeCommand(cmd);

        return cmd;
    }
}
