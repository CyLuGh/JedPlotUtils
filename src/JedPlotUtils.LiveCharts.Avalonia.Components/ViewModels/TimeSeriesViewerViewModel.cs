using System.Collections.ObjectModel;
using LanguageExt;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;

public partial class TimeSeriesViewerViewModel
    : JedPlotUtils.ViewModels.TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    private Seq<LineSeries<DateTimePoint>> _lineSeries;

    [ObservableAsProperty(ReadOnly = false)]
    private Seq<DateTimeAxis> _xAxes;

    public ReactiveCommand<Func<DateOnly, string>, Seq<DateTimeAxis>> CreateXAxisCommand { get; }

    public TimeSeriesViewerViewModel()
    {
        CreateXAxisCommand = CreateCommandCreateXAxisCommand();

        this.WhenAnyValue(x => x.DisplayMode)
            .Subscribe(x =>
            {
                Console.WriteLine(x);
            });

        _lineSeriesHelper = this.WhenAnyValue(x => x.SeriesCache)
            .CombineLatest(
                this.WhenAnyValue(x => x.Palette).Where(x => x is not null),
                this.WhenAnyValue(x => x.Selection)
            )
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Select(
                        (tsi, index) =>
                        {
                            float thickness = selection.IsEmpty
                                ? 2
                                : selection.Contains(tsi.Identifier)
                                    ? 3
                                    : 1;

                            var stroke =
                                selection.IsEmpty || selection.Contains(tsi.Identifier)
                                    ? new SolidColorPaint(
                                        palette.GetColor(index).Convert(),
                                        thickness
                                    )
                                    : new SolidColorPaint(
                                        palette.GetColor(index).Convert(),
                                        thickness
                                    )
                                    {
                                        PathEffect = new DashEffect([3, 2])
                                    };

                            return new LineSeries<DateTimePoint>()
                            {
                                Name = tsi.Label,
                                Values = new ObservableCollection<DateTimePoint>(
                                    tsi.Data.OrderBy(x => x.Key)
                                        .Select(x => new DateTimePoint(
                                            x.Key.ToDateTime(TimeOnly.MinValue),
                                            x.Value
                                        ))
                                ),
                                LineSmoothness = 0d,
                                Tag = tsi.Identifier,
                                Stroke = stroke,
                                Fill = null,
                                AnimationsSpeed = TimeSpan.Zero
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.LineSeries, scheduler: RxSchedulers.MainThreadScheduler);

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.DateFormatter)
                .Select(o =>
                    o.Match(
                        Signal.Return,
                        () => Signal.Return<Func<DateOnly, string>>(d => d.ToString("yyyy-MM"))
                    )
                )
                .Switch()
                .InvokeCommand(CreateXAxisCommand)
                .DisposeWith(disposables);
        });
    }

    private ReactiveCommand<
        Func<DateOnly, string>,
        Seq<DateTimeAxis>
    > CreateCommandCreateXAxisCommand()
    {
        var cmd = ReactiveCommand.Create<Func<DateOnly, string>, Seq<DateTimeAxis>>(f =>
            Seq.create(new DateTimeAxis(TimeSpan.FromDays(31), dt => f(DateOnly.FromDateTime(dt))))
        );

        _xAxesHelper = cmd.ToProperty(
            this,
            x => x.XAxes,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        return cmd;
    }
}
