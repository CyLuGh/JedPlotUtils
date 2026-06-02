using System.Reactive.Disposables.Fluent;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Markup.Xaml;
using JedPlotUtils.ScottPlot.Common.Components.ViewModels;
using ReactiveUI;
using ReactiveUI.Avalonia;

namespace JedPlotUtils.Scottplot.Avalonia.Components;

public partial class CsvParserView : ReactiveUserControl<CsvParserViewModel>
{
    public CsvParserView()
    {
        InitializeComponent();
    }
}
