using JDPlus.WS.Client;
using JDPlus.WS.Models;
using JedPlotUtils.Models;
using LanguageExt;
using LanguageExt.Common;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using ReactiveUI.SourceGenerators;
using Splat;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    public partial bool HasDisaggregationResults { get; }

    [ObservableAsProperty]
    public partial Option<TemporalDisaggregationResults> DisaggregationResults { get; }

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
        Either<Error, TemporalDisaggregationResults>
    > DisaggregateCommand { get; }

    public Interaction<
        RxVoid,
        Option<TemporalDisaggregationRequest>
    > GetDisaggregationRequestInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Either<Error, TemporalDisaggregationResults>
    > CreateCommandDisaggregateCommand(
        RxCommand clearSeriesCommand,
        RxCommand clearDerivedCommand,
        RxCommand cancelCommand
    )
    {
        GetDisaggregationRequestInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(
                new TemporalDisaggregationRequest()
                {
                    Model = "Rw",
                    Algorithm = "SqrtDiffuse",
                    NBackcasts = 0,
                    NForecasts = 6,
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

        var cmd = ReactiveCommand.CreateFromObservable(
            (Seq<TimeSeriesInfo> seq) =>
                Signal
                    .FromAsync(async ct =>
                    {
                        var req = await GetDisaggregationRequestInteraction.Handle(RxVoid.Default);
                        return await Disaggregate(seq.HeadOrNone(), req, ct).ConfigureAwait(false);
                    })
                    .TakeUntil(CancelCommand),
            canExecute
        );

        _disaggregationResultsHelper = Signal
            .Merge(
                cmd.Select(e => e.Match(r => r, _ => Option<TemporalDisaggregationResults>.None)),
                cmd.ThrownExceptions.Select(_ => Option<TemporalDisaggregationResults>.None),
                ClearResultsCommand.Select(_ => Option<TemporalDisaggregationResults>.None)
            )
            .ToProperty(
                this,
                x => x.DisaggregationResults,
                scheduler: RxSchedulers.MainThreadScheduler
            );

        cmd.Select(ei =>
                ei.Match(
                    _ => Signal.Empty<Message>(),
                    err => Signal.Return(new Message("Disaggregate", err.Message))
                )
            )
            .SwitchTo()
            .InvokeCommand(Warn);
        cmd.ThrownExceptions.Select(exc => new Message("Disaggregate", exc)).InvokeCommand(Panic);

        return cmd;
    }

    private async Task<Either<Error, TemporalDisaggregationResults>> Disaggregate(
        Option<TimeSeriesInfo> oSeries,
        Option<TemporalDisaggregationRequest> oRequest,
        CancellationToken token
    )
    {
        var elements =
            from cm in WsManager.ToOption()
            from tsi in oSeries
            from req in oRequest
            select (cm, tsi, req);

        var derived = (
            await elements
                .ToEither(
                    Error.New(
                        "Couldn't process disaggregation: either connection or request is missing."
                    )
                )
                .BindAsync(async t =>
                {
                    var (cm, tsi, req) = t;
                    var data = tsi.Data.ToSeq();
                    await Clear(true).ConfigureAwait(false);

                    var tsData = await cm.BuildTsData(
                        data,
                        aggregationType: AggregationType.None,
                        tsi.Frequency.Match(f => f, () => Frequency.Undefined),
                        token: token
                    );

                    var disaggregated = await tsData.BindAsync(async td =>
                        await cm.ProcessTemporalDisaggregation(req with { Y = td }, token)
                    );

                    return disaggregated;
                })
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
                            level: Level.Quaternary
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
