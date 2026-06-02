using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Text;
using DynamicData;
using HierarchyGrid.Definitions;
using JedPlotUtils.Models;
using LanguageExt;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public abstract partial class TimeSeriesViewerViewModelBase : BaseViewModel
{
    protected readonly SourceCache<TimeSeriesInfo, Identifier> _seriesCache =
        new(x => x.Identifier);
    protected readonly IObservable<IChangeSet<TimeSeriesInfo, Identifier>> _cacheUpdates;

    protected readonly ReadOnlyObservableCollection<TimeSeriesInfo> _seriesInfos;
    public ReadOnlyObservableCollection<TimeSeriesInfo> SeriesInfos => _seriesInfos;

    public HierarchyGridViewModel HierarchyGridViewModel { get; } = new();

    [Reactive]
    public partial Option<(Identifier, DateOnly)> HoveredPoint { get; set; }

    protected TimeSeriesViewerViewModelBase()
    {
        _cacheUpdates = _seriesCache.Connect().RefCount();

        //.Bind(out _seriesInfos);
    }

    public void Add(TimeSeriesInfo tsi)
    {
        _seriesCache.AddOrUpdate(tsi);
    }

    public void Clear()
    {
        _seriesCache.Clear();
    }
}
