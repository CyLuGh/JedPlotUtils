using System.Collections.ObjectModel;
using JedPlotUtils.Models;
using LanguageExt;
using LiveChartsCore;
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

    [ObservableAsProperty]
    private Seq<RangeLineSeries<DateTimeRangeValue>> _rangeSeries;

    [ObservableAsProperty]
    private Seq<ISeries> _allSeries;

    [ObservableAsProperty(ReadOnly = false)]
    private Seq<DateTimeAxis> _xAxes;

    public ReactiveCommand<Func<DateOnly, string>, Seq<DateTimeAxis>> CreateXAxisCommand { get; }

    public TimeSeriesViewerViewModel()
    {
        CreateXAxisCommand = CreateCommandCreateXAxisCommand();

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
                    .Values.Where(tsi => tsi.ChartType != SeriesChartType.Range)
                    .Select(
                        (tsi) =>
                        {
                            float thickness = selection.IsEmpty
                                ? 2
                                : selection.Contains(tsi.Identifier)
                                    ? 3
                                    : 1;

                            var stroke =
                                selection.IsEmpty || selection.Contains(tsi.Identifier)
                                    ? new SolidColorPaint(
                                        palette.GetColor(tsi.Index).Convert(),
                                        thickness
                                    )
                                    : new SolidColorPaint(
                                        palette.GetColor(tsi.Index).Convert(),
                                        thickness
                                    )
                                    {
                                        PathEffect = new DashEffect([3, 2])
                                    };

                            var fill =
                                tsi.ChartType == SeriesChartType.Area
                                    ? new SolidColorPaint(
                                        palette
                                            .GetColor(tsi.Index)
                                            .Convert()
                                            .WithAlpha(
                                                selection.IsEmpty
                                                || selection.Contains(tsi.Identifier)
                                                    ? (byte)60
                                                    : (byte)20
                                            )
                                    )
                                    : null;

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
                                Fill = fill,
                                AnimationsSpeed = TimeSpan.Zero,
                                GeometrySize = GetGeometrySize(tsi),
                                IsHoverable = tsi.Level != Level.Tertiary
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.LineSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _rangeSeriesHelper = this.WhenAnyValue(x => x.SeriesCache)
            .CombineLatest(
                this.WhenAnyValue(x => x.Palette).Where(x => x is not null),
                this.WhenAnyValue(x => x.Selection)
            )
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Where(tsi =>
                        tsi is { ChartType: SeriesChartType.Range, AuxiliaryData.IsEmpty: false }
                    )
                    .Select(
                        (tsi) =>
                        {
                            float thickness = selection.IsEmpty
                                ? 2
                                : selection.Contains(tsi.Identifier)
                                    ? 3
                                    : 1;

                            var stroke =
                                selection.IsEmpty || selection.Contains(tsi.Identifier)
                                    ? new SolidColorPaint(
                                        palette.GetColor(tsi.Index).Convert().WithAlpha(60),
                                        thickness
                                    )
                                    : new SolidColorPaint(
                                        palette.GetColor(tsi.Index).Convert().WithAlpha(60),
                                        thickness
                                    )
                                    {
                                        PathEffect = new DashEffect([3, 2])
                                    };

                            var fill =
                                tsi.ChartType == SeriesChartType.Range
                                    ? new SolidColorPaint(
                                        palette
                                            .GetColor(tsi.Index)
                                            .Convert()
                                            .WithAlpha(
                                                selection.IsEmpty
                                                || selection.Contains(tsi.Identifier)
                                                    ? (byte)40
                                                    : (byte)20
                                            )
                                    )
                                    : null;

                            return new RangeLineSeries<DateTimeRangeValue>()
                            {
                                Name = tsi.Label,
                                Values = new ObservableCollection<DateTimeRangeValue>(
                                    tsi.Data.OrderBy(x => x.Key)
                                        .Select(x => new DateTimeRangeValue(
                                            x.Key.ToDateTime(TimeOnly.MinValue),
                                            tsi.AuxiliaryData.Find(x.Key)
                                                .MatchUnsafe<double?>(d => d, () => null),
                                            x.Value
                                        ))
                                ),
                                LineSmoothness = 0d,
                                Tag = tsi.Identifier,
                                Stroke = stroke,
                                Fill = fill,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level != Level.Tertiary,
                                GeometrySize = GetGeometrySize(tsi)
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.RangeSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _allSeriesHelper = this.WhenAnyValue(x => x.LineSeries)
            .CombineLatest(this.WhenAnyValue(x => x.RangeSeries))
            .Select(t => t.First.Cast<ISeries>().Concat(t.Second.Cast<ISeries>()))
            .ToProperty(this, x => x.AllSeries, scheduler: RxSchedulers.MainThreadScheduler);

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

    private static double GetGeometrySize(TimeSeriesInfo tsi) =>
        tsi.Level switch
        {
            Level.Primary => tsi.ChartType == SeriesChartType.Line ? 12d : 6d,
            Level.Secondary => 4d,
            Level.Tertiary => 0d,
            _ => 12d
        };

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
