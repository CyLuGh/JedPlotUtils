using JDPlus.WS.Models;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using LanguageExt.Common;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    public partial Option<TramoForecasts> TramoForecast { get; }

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TramoForecasts>
    > TramoForecastCommand { get; }

    public Interaction<RxVoid, Option<TramoRequest>> GetTramoRequestInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TramoForecasts>
    > CreateCommandTramoForecast()
    {
        GetTramoRequestInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(new TramoRequest() { Spec = "TRfull", NForecasts = 3 })
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
            async (Seq<TimeSeriesInfo> series) =>
            {
                var request = await GetTramoRequestInteraction.Handle(RxVoid.Default);
                var tf = await GetTramoForecast(series.HeadOrNone(), request);
                return tf;
            },
            canExecute
        );

        _tramoForecastHelper = Signal
            .Merge(
                cmd,
                ClearResultsCommand.Select(_ => Option<TramoForecasts>.None),
                cmd.ThrownExceptions.Select(_ => Option<TramoForecasts>.None)
            )
            .ToProperty(this, x => x.TramoForecast, scheduler: RxSchedulers.MainThreadScheduler);

        return cmd;
    }

    private async Task<Option<TramoForecasts>> GetTramoForecast(
        Option<TimeSeriesInfo> oSeries,
        Option<TramoRequest> oRequest
    )
    {
        var elements =
            from cm in WsManager
            from tsi in oSeries
            from request in oRequest
            select (cm, tsi, request);

        var tf = await elements.MatchAsync(
            async e =>
            {
                var (cm, tsi, req) = e;
                await Clear(true).ConfigureAwait(false);
                var data = tsi.Data.ToSeq();
                var tsData = await cm.BuildTsData(
                    data,
                    aggregationType: AggregationType.None,
                    tsi.Frequency.Match(f => f, () => Frequency.Undefined)
                );

                return await tsData
                    .MatchAsync(
                        async r =>
                        {
                            var res = await cm.GetTramoForecasts(
                                    r,
                                    req.Spec,
                                    req.NForecasts,
                                    Option<ModellingContext>.None
                                )
                                .ConfigureAwait(false);
                            return Option<TramoForecasts>.Some(res);
                        },
                        error => Option<TramoForecasts>.None
                    )
                    .ConfigureAwait(false);
            },
            () => Task.FromResult(Option<TramoForecasts>.None)
        );

        return tf;
    }

    public ReactiveCommand<
        Option<TramoForecasts>,
        RxVoid
    > CreateTramoForecastsSeriesCommand { get; }

    private ReactiveCommand<
        Option<TramoForecasts>,
        RxVoid
    > CreateCommandCreateTramoForecastsSeriesCommand()
    {
        var cmd = ReactiveCommand.CreateFromTask(
            async (Option<TramoForecasts> oForecasts) =>
            {
                oForecasts.IfSome(forecasts =>
                {
                    var fc = forecasts.Forecasts.Select(x => (x.Key, x.Value.Forecast)).ToHashMap();
                    var stdev = forecasts
                        .Forecasts.Select(x => (x.Key, x.Value.ForecastStDev))
                        .ToHashMap();

                    Add(
                        new TimeSeriesInfo(
                            stdev.Select(t => (t.Key, fc.Find(t.Key, d => d, () => 0d) + t.Value)),
                            stdev.Select(t => (t.Key, fc.Find(t.Key, d => d, () => 0d) - t.Value)),
                            level: Level.Quaternary
                        )
                    );

                    Add(new TimeSeriesInfo("Forecasts", fc, level: Level.Secondary));
                });
            }
        );

        this.WhenAnyValue(x => x.TramoForecast).InvokeCommand(cmd);
        return cmd;
    }

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        HashMap<Identifier, Either<Error, TramoOutput>>
    > TramoFullProcessCommand { get; }

    [ObservableAsProperty]
    public partial HashMap<Identifier, Either<Error, TramoOutput>> TramoOutput { get; }

    [ObservableAsProperty]
    public partial HashMap<
        (Identifier, DateOnly),
        Either<Error, TramoOutput.OutlierReport>
    > TramoOutliers { get; }

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        HashMap<Identifier, Either<Error, TramoOutput>>
    > CreateCommandTramoFullProcessCommand()
    {
        var canExecute = this.WhenAnyValue(x => x.HasConnection)
            .ObserveOn(RxSchedulers.MainThreadScheduler);

        var cmd = ReactiveCommand.CreateFromTask(
            async (Seq<TimeSeriesInfo> series) =>
            {
                /* Process all series if none are selected */
                if (series.IsEmpty)
                    series = SeriesCache.Values.ToSeq();

                var request = await GetTramoRequestInteraction.Handle(RxVoid.Default);
                var tf = await GetTramoFullProcess(series, request);
                return tf;
            },
            canExecute
        );

        _tramoOutputHelper = cmd.ToProperty(
            this,
            x => x.TramoOutput,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        return cmd;
    }

    private async Task<HashMap<Identifier, Either<Error, TramoOutput>>> GetTramoFullProcess(
        Seq<TimeSeriesInfo> series,
        Option<TramoRequest> oRequest
    )
    {
        var elements = from cm in WsManager from request in oRequest select (cm, request);

        var to = await elements.MatchAsync(
            async e =>
            {
                var (cm, req) = e;

                List<(Identifier id, Either<Error, TramoOutput> output)> processed = [];

                foreach (var tsi in series.Where(s => s.Level == Level.Primary))
                {
                    var data = tsi.Data.ToSeq();
                    var tsData = await cm.BuildTsData(
                        data,
                        aggregationType: AggregationType.None,
                        tsi.Frequency.Match(f => f, () => Frequency.Undefined)
                    );

                    await tsData.Match(
                        async r =>
                        {
                            var res = await cm.GetTramoFullProcess(
                                    r,
                                    req.Spec,
                                    Option<ModellingContext>.None
                                )
                                .ConfigureAwait(false);

                            processed.Add((tsi.Identifier, res));
                        },
                        async error =>
                        {
                            processed.Add((tsi.Identifier, error));
                        }
                    );
                }

                return processed.ToHashMap();
            },
            () => Task.FromResult(HashMap<Identifier, Either<Error, TramoOutput>>.Empty)
        );

        return to;
    }

    public ReactiveCommand<
        HashMap<Identifier, Either<Error, TramoOutput>>,
        RxVoid
    > CreateTramoOutputSeriesCommand { get; }

    private ReactiveCommand<
        HashMap<Identifier, Either<Error, TramoOutput>>,
        RxVoid
    > CreateCommandCreateTramoOutputSeriesCommand()
    {
        var cmd = ReactiveCommand.CreateFromTask(
            async (HashMap<Identifier, Either<Error, TramoOutput>> map) =>
            {
                await Clear(true).ConfigureAwait(false);

                foreach (var (id, output) in map)
                {
                    var tsis = output
                        .Match(x => x.GetOutliers(), _ => Seq<TramoOutput.OutlierReport>.Empty)
                        .GroupBy(x => x.Code)
                        .Select(g => new TimeSeriesInfo(
                            g.Key,
                            g.Select(x => (x.Period, x.Value)).ToHashMap(),
                            level: Level.Tertiary,
                            chartType: SeriesChartType.Scatter
                        )
                        {
                            Color = OutliersHighlight.GetOutlierColor(g.Key)
                        })
                        .ToSeq()
                        .Strict();

                    Add(tsis);
                }
            }
        );

        this.WhenAnyValue(x => x.TramoOutput).InvokeCommand(cmd);
        return cmd;
    }

    public ReactiveCommand<
        (HashMap<Identifier, Either<Error, TramoOutput>>, HashMap<Identifier, TimeSeriesInfo>),
        HashMap<(Identifier, DateOnly), Either<Error, TramoOutput.OutlierReport>>
    > BuildOutliersMap { get; }
}
