using System;
using System.Collections.Generic;
using System.Text;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using JedPlotUtils.ViewModels;
using ReactiveUI;
using ReactiveUI.SourceGenerators;
using RxCommand = ReactiveUI.ReactiveCommand<
    ReactiveUI.Primitives.RxVoid,
    ReactiveUI.Primitives.RxVoid
>;

namespace LC_AvaloniaApplication;

public partial class DemoViewModel : BaseViewModel
{
    [Reactive]
    public partial IPalette? Palette { get; set; }

    [Reactive]
    public partial SelectionMode SelectionMode { get; set; }

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }

    public TimeSeriesViewerViewModel TimeSeriesViewModel { get; }
    private readonly Random _random;

    public RxCommand AddSeries { get; }
    public RxCommand AddSeriesWithAuxiliaryData { get; }
    public RxCommand ClearSeries { get; }

    public DemoViewModel()
    {
        TimeSeriesViewModel = new TimeSeriesViewerViewModel();
        _random = new Random(0);

        AddSeries = ReactiveCommand.Create(
            () =>
                TimeSeriesViewModel.Add(
                    GenerateTimeSeriesInfo("A", new DateOnly(2000, 1, 1), 24, d => d.AddMonths(3))
                )
        );
        AddSeriesWithAuxiliaryData = ReactiveCommand.Create(
            () =>
                TimeSeriesViewModel.Add(
                    GenerateTimeSeriesInfoWithAuxiliaryData(
                        "A",
                        new DateOnly(2000, 1, 1),
                        24,
                        d => d.AddMonths(1)
                    )
                )
        );
        ClearSeries = ReactiveCommand.Create(() => TimeSeriesViewModel.Clear(false));
    }

    private TimeSeriesInfo GenerateTimeSeriesInfo(
        string label,
        DateOnly start,
        int count,
        Func<DateOnly, DateOnly> increment
    )
    {
        var current = start;
        var dates = new List<DateOnly>();
        var values = new List<double>();

        while (dates.Count < count)
        {
            dates.Add(current);
            values.Add(_random.NextDouble() * 1_000_000);
            current = increment(current);
        }

        return new(label, [.. dates], [.. values]);
    }

    private TimeSeriesInfo GenerateTimeSeriesInfoWithAuxiliaryData(
        string label,
        DateOnly start,
        int count,
        Func<DateOnly, DateOnly> increment
    )
    {
        var current = start;
        var dates = new List<DateOnly>();
        var values = new List<double>();
        var auxiliaries = new List<double>();

        while (dates.Count < count)
        {
            dates.Add(current);
            var a = _random.NextDouble() * 1_000_000;
            var b = _random.NextDouble() * 1_000_000;
            values.Add(Math.Max(a, b));
            auxiliaries.Add(Math.Min(a, b));
            current = increment(current);
        }

        return new(label, [.. dates], [.. values], [.. auxiliaries]);
    }
}
