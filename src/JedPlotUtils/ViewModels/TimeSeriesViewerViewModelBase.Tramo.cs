using JDPlus.WS.Models;
using JedPlotUtils.Models;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    public partial Option<TramoForecasts> TramoForecast { get; }

    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TramoForecasts>
    > TramoForecastCommand { get; }

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<TramoForecasts>
    > CreateCommandTramoForecast()
    {
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
                var tf = await GetTramoForecast(series.HeadOrNone());
                return tf;
            },
            canExecute
        );

        cmd.ToProperty(this, x => x.TramoForecast, scheduler: RxSchedulers.MainThreadScheduler);

        return cmd;
    }

    private async Task<Option<TramoForecasts>> GetTramoForecast(Option<TimeSeriesInfo> oSeries)
    {
        var elements = from cm in WsManager from tsi in oSeries select (cm, tsi);

        var tf = await elements.MatchAsync(
            async e =>
            {
                var (cm, tsi) = e;

                var data = tsi.Data.ToSeq();

                var tsData = await cm.BuildTsData(
                    data,
                    aggregationType: AggregationType.None,
                    Frequency.Undefined
                );

                var res = await cm.GetTramoForecasts(
                        tsData,
                        "TRfull",
                        3,
                        Option<ModellingContext>.None
                    )
                    .ConfigureAwait(false);
                return Option<TramoForecasts>.Some(res);
            },
            () => Task.FromResult(Option<TramoForecasts>.None)
        );

        return tf;
    }
}
