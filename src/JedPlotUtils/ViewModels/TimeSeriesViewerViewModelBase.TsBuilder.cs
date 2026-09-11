using System.Diagnostics.Contracts;
using JDPlus.WS.Client;
using JedPlotUtils.Models;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    public ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<(TimeSeriesInfo, Option<Identifier>)>
    > ChangeFrequencyCommand { get; }

    public Interaction<
        TimeSeriesInfo,
        Option<TimeSeriesBuildOptions>
    > GetFrequencyChangeOptionsInteraction { get; } = new(RxSchedulers.MainThreadScheduler);

    private ReactiveCommand<
        Seq<TimeSeriesInfo>,
        Option<(TimeSeriesInfo, Option<Identifier>)>
    > CreateCommandChangeFrequencyCommand()
    {
        GetFrequencyChangeOptionsInteraction.RegisterHandler(ctx =>
            ctx.SetOutput(Option<TimeSeriesBuildOptions>.None)
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
                var buildOptions = await GetFrequencyChangeOptionsInteraction.Handle(seq[0]);

                var elements =
                    from cm in WsManager
                    from options in buildOptions
                    select (cm, options);

                return await elements.MatchAsync(
                    async t =>
                    {
                        var (cm, options) = t;
                        var tsi = await ChangeFrequency(cm, seq[0], options);
                        return Option<(TimeSeriesInfo, Option<Identifier>)>.Some(
                            (
                                tsi,
                                options.RemoveOriginal ? seq[0].Identifier : Option<Identifier>.None
                            )
                        );
                    },
                    () => Option<(TimeSeriesInfo, Option<Identifier>)>.None
                );
            },
            canExecute
        );

        cmd.Merge(
                cmd.ThrownExceptions.Select(_ => Option<(TimeSeriesInfo, Option<Identifier>)>.None)
            )
            .Select(o => o.Match(Signal.Return, Signal.Empty<(TimeSeriesInfo, Option<Identifier>)>))
            .Switch()
            .Subscribe(t =>
            {
                var (tsi, removed) = t;

                removed.IfSome(r => Remove(r));
                Add(tsi);
            });

        return cmd;
    }

    [Pure]
    private static async Task<TimeSeriesInfo> ChangeFrequency(
        CommunicationManager cm,
        TimeSeriesInfo source,
        TimeSeriesBuildOptions buildOptions
    )
    {
        var tsData = await cm.BuildTsData(
            source.Data.Select(x => (x.Key, x.Value)).ToSeq(),
            buildOptions.AggregationType,
            buildOptions.Frequency
        );

        return new TimeSeriesInfo(
            $"{source.Label} ({buildOptions.Frequency})",
            tsData.GetDateValues()
        );
    }
}
