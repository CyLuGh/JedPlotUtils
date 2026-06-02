using System.Collections.ObjectModel;
using System.Reactive.Linq;
using DynamicData;
using LiveChartsCore.Defaults;
using LiveChartsCore.SkiaSharpView;
using ReactiveUI;

namespace JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;

public class TimeSeriesViewerViewModel : JedPlotUtils.ViewModels.TimeSeriesViewerViewModelBase
{
    private readonly ReadOnlyObservableCollection<LineSeries<ObservablePoint>> _lineSeries;
    public ReadOnlyObservableCollection<LineSeries<ObservablePoint>> LineSeries => _lineSeries;

    public TimeSeriesViewerViewModel()
        : base()
    {
        _cacheUpdates
            .ObserveOn(RxSchedulers.TaskpoolScheduler)
            .Transform(tsi =>
            {
                var xls = new LineSeries<ObservablePoint>()
                {
                    Name = tsi.Label,
                    Values = new ObservableCollection<ObservablePoint>(
                        tsi.Data.OrderBy(x => x.Key)
                            .Select(x => new ObservablePoint(x.Key.ToOADate(), x.Value))
                    ),
                    LineSmoothness = 0d,
                    Tag = tsi.Identifier,
                };
                return xls;
            })
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Bind(out _lineSeries)
            .DisposeMany()
            .Subscribe();
    }
}
