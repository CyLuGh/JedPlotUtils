using System;
using System.Collections.Generic;
using Avalonia.Controls;
using Avalonia.Interactivity;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;

namespace LC_AvaloniaApplication;

public partial class MainWindow : Window
{
    private readonly TimeSeriesViewerViewModel _simpleViewModel;
    private readonly Random _random;

    public MainWindow()
    {
        InitializeComponent();

        _random = new Random(0);
        _simpleViewModel = new TimeSeriesViewerViewModel();
        TimeSeriesViewer.ViewModel = _simpleViewModel;
    }

    private void AddSeries(object? sender, RoutedEventArgs e)
    {
        _simpleViewModel.Add(
            GenerateTimeSeriesInfo("A", new DateOnly(2000, 1, 1), 24, d => d.AddMonths(1))
        );
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

    private void ClearSeries(object? sender, RoutedEventArgs e)
    {
        _simpleViewModel.Clear();
    }
}
