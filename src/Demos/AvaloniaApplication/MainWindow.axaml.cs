using System;
using Avalonia.Controls;
using DemoData;

namespace AvaloniaApplication
{
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();

            SimpleView.ViewModel = new();
            TimeSeriesView.ViewModel = new();
        }

        private int _count = 0;

        private void AddSomeData_Click(object? sender, Avalonia.Interactivity.RoutedEventArgs e)
        {
            var infos = new TsGenerator().GenerateTimeSeriesInfo(
                $"Series {++_count}",
                new DateOnly(2016, 1, 1),
                120 / _count,
                d => d.AddMonths(1 * _count)
            );

            TimeSeriesView.ViewModel!.AddTimeSeriesInfo(infos);
        }
    }
}
