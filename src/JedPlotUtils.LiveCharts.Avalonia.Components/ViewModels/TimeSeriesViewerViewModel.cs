using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using LanguageExt;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using LiveChartsCore.SkiaSharpView.Painting;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;

public partial class TimeSeriesViewerViewModel
    : JedPlotUtils.ViewModels.TimeSeriesViewerViewModelBase
{
    [ObservableAsProperty]
    private Seq<LineSeries<DateTimePoint>> _lineSeries;

    public TimeSeriesViewerViewModel()
        : base()
    {
        _lineSeriesHelper = _cacheUpdates
            .DisposeMany()
            .CombineLatest(this.WhenAnyValue(x => x.Palette), this.WhenAnyValue(x => x.Selection))
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Select(t =>
            {
                var (_, palette, selection) = t;
                return _seriesCache
                    .Items.Select(
                        (tsi, index) =>
                        {
                            float thickness = selection.IsEmpty
                                ? 2
                                : selection.Contains(tsi.Identifier)
                                    ? 3
                                    : 1;
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
                                Stroke = new SolidColorPaint(
                                    palette.GetColor(index).Convert(),
                                    thickness
                                ),
                                AnimationsSpeed = TimeSpan.Zero
                            };
                        }
                    )
                    .ToSeq();
            })
            .ToProperty(this, x => x.LineSeries, scheduler: RxSchedulers.MainThreadScheduler);
    }
}
