using System.Collections.ObjectModel;
using System.Globalization;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using LiveChartsCore;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using LiveChartsCore.SkiaSharpView.Painting.Effects;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Signals;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;

public partial class TimeSeriesViewerViewModel
    : JedPlotUtils.ViewModels.TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    public partial Seq<LineSeries<DateTimePoint>> LineSeries { get; }

    [ObservableAsProperty]
    public partial Seq<StepLineSeries<DateTimePoint>> StepLineSeries { get; }

    [ObservableAsProperty]
    public partial Seq<RangeLineSeries<DateTimeRangeValue>> RangeSeries { get; }

    [ObservableAsProperty]
    public partial Seq<ColumnSeries<DateTimePoint>> ColumnSeries { get; }

    [ObservableAsProperty]
    public partial Seq<ScatterSeries<DateTimePoint>> ScatterSeries { get; }

    [ObservableAsProperty]
    public partial Seq<ISeries> AllSeries { get; }

    [ObservableAsProperty(ReadOnly = false)]
    public partial Seq<DateTimeAxis> XAxes { get; }

    [ObservableAsProperty(ReadOnly = false)]
    public partial Seq<Axis> YAxes { get; }

    public ReactiveCommand<Func<DateOnly, string>, Seq<DateTimeAxis>> CreateXAxisCommand { get; }
    public ReactiveCommand<Func<double, string>, Seq<Axis>> CreateYAxisCommand { get; }

    public TimeSeriesViewerViewModel()
    {
        CreateXAxisCommand = CreateCommandCreateXAxisCommand();
        CreateYAxisCommand = CreateCommandCreateYAxisCommand();

        var redrawObservable = this.WhenAnyValue(x => x.SeriesCache)
            .CombineLatest(
                this.WhenAnyValue(x => x.Palette).Where(x => x is not null),
                this.WhenAnyValue(x => x.Selection)
            )
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Publish()
            .RefCount();

        _lineSeriesHelper = redrawObservable
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Where(tsi =>
                        tsi.ChartType is SeriesChartType.Line or SeriesChartType.Area
                    )
                    .Select(
                        (tsi) =>
                        {
                            var di = ChartUtils.GetInfos(tsi, selection, palette);

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
                                Stroke = di.Stroke,
                                Fill = di.Fill,
                                GeometrySize = di.GeometrySize,
                                GeometryStroke = di.GeometryStroke,
                                GeometryFill = di.GeometryFill,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level < Level.Tertiary,
                                YToolTipLabelFormatter = point =>
                                    tsi.NumberFormat.Match(
                                        f => point.Model?.Value?.ToString(f) ?? string.Empty,
                                        () => point.Model?.Value?.ToString("N2") ?? string.Empty
                                    ),
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.LineSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _stepLineSeriesHelper = redrawObservable
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Where(tsi => tsi.ChartType == SeriesChartType.StepLine)
                    .Select(
                        (tsi) =>
                        {
                            var di = ChartUtils.GetInfos(tsi, selection, palette);

                            return new StepLineSeries<DateTimePoint>()
                            {
                                Name = tsi.Label,
                                Values = new ObservableCollection<DateTimePoint>(
                                    tsi.Data.OrderBy(x => x.Key)
                                        .Select(x => new DateTimePoint(
                                            x.Key.ToDateTime(TimeOnly.MinValue),
                                            x.Value
                                        ))
                                ),
                                Tag = tsi.Identifier,
                                Stroke = di.Stroke,
                                Fill = di.Fill,
                                GeometrySize = di.GeometrySize,
                                GeometryStroke = di.GeometryStroke,
                                GeometryFill = di.GeometryFill,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level < Level.Tertiary,
                                YToolTipLabelFormatter = point =>
                                    tsi.NumberFormat.Match(
                                        f => point.Model?.Value?.ToString(f) ?? string.Empty,
                                        () => point.Model?.Value?.ToString("N2") ?? string.Empty
                                    ),
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.StepLineSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _rangeSeriesHelper = redrawObservable
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
                            var di = ChartUtils.GetInfos(tsi, selection, palette);

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
                                Stroke = di.Stroke,
                                Fill = di.Fill,
                                GeometrySize = di.GeometrySize,
                                GeometryStroke = di.GeometryStroke,
                                GeometryFill = di.GeometryFill,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level < Level.Tertiary,
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.RangeSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _columnSeriesHelper = redrawObservable
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Where(tsi => tsi.ChartType == SeriesChartType.Column)
                    .Select(
                        (tsi) =>
                        {
                            var di = ChartUtils.GetInfos(tsi, selection, palette);

                            return new ColumnSeries<DateTimePoint>()
                            {
                                Name = tsi.Label,
                                Values = new ObservableCollection<DateTimePoint>(
                                    tsi.Data.OrderBy(x => x.Key)
                                        .Select(x => new DateTimePoint(
                                            x.Key.ToDateTime(TimeOnly.MinValue),
                                            x.Value
                                        ))
                                ),
                                Tag = tsi.Identifier,
                                Stroke = di.Stroke,
                                Fill = di.Fill,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level < Level.Tertiary,
                                YToolTipLabelFormatter = point =>
                                    tsi.NumberFormat.Match(
                                        f => point.Model?.Value?.ToString(f) ?? string.Empty,
                                        () => point.Model?.Value?.ToString("N2") ?? string.Empty
                                    ),
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.ColumnSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _scatterSeriesHelper = redrawObservable
            .Select(t =>
            {
                var (map, palette, selection) = t;
                return map
                    .Values.Where(tsi => tsi.ChartType is SeriesChartType.Scatter)
                    .Select(
                        (tsi) =>
                        {
                            var di = ChartUtils.GetInfos(tsi, selection, palette);

                            return new ScatterSeries<DateTimePoint>()
                            {
                                Name = tsi.Label,
                                Values = new ObservableCollection<DateTimePoint>(
                                    tsi.Data.OrderBy(x => x.Key)
                                        .Select(x => new DateTimePoint(
                                            x.Key.ToDateTime(TimeOnly.MinValue),
                                            x.Value
                                        ))
                                ),
                                Tag = tsi.Identifier,
                                Stroke = di.Stroke,
                                Fill = di.Fill,
                                GeometrySize = di.GeometrySize,
                                AnimationsSpeed = TimeSpan.Zero,
                                IsHoverable = tsi.Level < Level.Tertiary,
                                YToolTipLabelFormatter = point =>
                                    tsi.NumberFormat.Match(
                                        f => point.Model?.Value?.ToString(f) ?? string.Empty,
                                        () => point.Model?.Value?.ToString("N2") ?? string.Empty
                                    ),
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.ScatterSeries, scheduler: RxSchedulers.MainThreadScheduler);

        _allSeriesHelper = this.WhenAnyValue(x => x.LineSeries)
            .CombineLatest(
                this.WhenAnyValue(x => x.StepLineSeries),
                this.WhenAnyValue(x => x.RangeSeries),
                this.WhenAnyValue(x => x.ColumnSeries),
                this.WhenAnyValue(x => x.ScatterSeries)
            )
            .Select(t =>
                t.First.Cast<ISeries>()
                    .Concat(t.Second.Cast<ISeries>())
                    .Concat(t.Third.Cast<ISeries>())
                    .Concat(t.Fourth.Cast<ISeries>())
                    .Concat(t.Fifth.Cast<ISeries>())
            )
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
                .SwitchTo()
                .InvokeCommand(CreateXAxisCommand)
                .DisposeWith(disposables);

            this.WhenAnyValue(x => x.NumberFormat)
                .Select<string?, Func<double, string>>(f =>
                    !string.IsNullOrWhiteSpace(f)
                        ? d => d.ToString(f)
                        : d => d.ToString(CultureInfo.InvariantCulture)
                )
                .InvokeCommand(CreateYAxisCommand)
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

    private ReactiveCommand<Func<double, string>, Seq<Axis>> CreateCommandCreateYAxisCommand()
    {
        var cmd = ReactiveCommand.Create<Func<double, string>, Seq<Axis>>(f =>
            Seq.create(new Axis() { Labeler = f })
        );

        _yAxesHelper = cmd.ToProperty(
            this,
            x => x.YAxes,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        return cmd;
    }
}
