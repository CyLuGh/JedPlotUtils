using System;
using System.Collections.Generic;
using System.Globalization;
using System.Reactive.Disposables.Fluent;
using System.Reactive.Linq;
using System.Text;
using ReactiveUI;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ScottPlot.Common.Components.ViewModels;

public partial class CsvParserViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();

    public string[] Separators { get; } = [",", ";", "\\t"];

    [Reactive]
    public partial string Separator { get; set; }

    public string[] DecimalSeparators { get; } = [".", ","];

    [Reactive]
    public partial string DecimalSeparator { get; set; }

    [Reactive]
    public partial bool HasHeaders { get; set; }

    [Reactive]
    public partial string FilePath { get; set; }

    [Reactive]
    public partial int PeriodIndex { get; set; }

    [Reactive]
    public partial int[] DataColumns { get; set; }

    public ReactiveCommand<
        (string, string, Func<string, double>, bool, int),
        CsvColumnSelectorViewModel[]
    > ParseFirstLineCommand { get; }

    [ObservableAsProperty(ReadOnly = false)]
    private CsvColumnSelectorViewModel[] _dataPreview;

    [ObservableAsProperty(ReadOnly = false)]
    private int[] _columns;

    [ObservableAsProperty(ReadOnly = false)]
    private Func<string, double> _doubleConverter;

    public CsvParserViewModel()
    {
        ParseFirstLineCommand = CreateCommandParseFirstLineCommand();

        Separator = ",";
        DecimalSeparator = ".";

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.FilePath)
                .CombineLatest(
                    this.WhenAnyValue(x => x.Separator),
                    this.WhenAnyValue(x => x.DoubleConverter).WhereNotNull(),
                    this.WhenAnyValue(x => x.HasHeaders),
                    this.WhenAnyValue(x => x.PeriodIndex).DistinctUntilChanged()
                )
                .InvokeCommand(ParseFirstLineCommand)
                .DisposeWith(disposables);

            _doubleConverterHelper = this.WhenAnyValue(x => x.DecimalSeparator)
                .Select(x => x == "." ? DotConverter : CommaConverter)
                .ToProperty(this, x => x.DoubleConverter)
                .DisposeWith(disposables);
        });
    }

    private ReactiveCommand<
        (string, string, Func<string, double>, bool, int),
        CsvColumnSelectorViewModel[]
    > CreateCommandParseFirstLineCommand()
    {
        var cmd = ReactiveCommand.CreateFromTask(
            ((string, string, Func<string, double>, bool, int) t) =>
            {
                var (path, sep, dec, hasHeaders, periodIndex) = t;
                return DoParseFirstLine(path, sep, dec, hasHeaders, periodIndex);
            }
        );

        _dataPreviewHelper = cmd.ToProperty(
            this,
            x => x.DataPreview,
            scheduler: RxSchedulers.MainThreadScheduler
        );

        _columnsHelper = cmd.Select(c => Enumerable.Range(1, c.Length).ToArray())
            .ToProperty(this, x => x.Columns, scheduler: RxSchedulers.MainThreadScheduler);

        this.WhenAnyValue(x => x.Columns)
            .Where(cols => cols?.Length > 0 && (PeriodIndex == 0 || PeriodIndex > cols.Length))
            .ObserveOn(RxSchedulers.MainThreadScheduler)
            .Subscribe(cols =>
            {
                PeriodIndex = 1;
            });

        return cmd;
    }

    private async Task<CsvColumnSelectorViewModel[]> DoParseFirstLine(
        string filePath,
        string separator,
        Func<string, double> doubleConverter,
        bool hasHeaders,
        int periodIndex
    )
    {
        if (!File.Exists(filePath))
            return [];

        var lines = await File.ReadAllLinesAsync(filePath);

        if (lines.Length == 0)
            return [];

        CsvColumnSelectorViewModel[] ToSelector(string[] elements) =>
            [
                .. elements.Select(
                    (s, i) =>
                        new CsvColumnSelectorViewModel
                        {
                            Content = s,
                            Index = i,
                            PeriodIndex = periodIndex - 1,
                            Converter = doubleConverter
                        }
                )
            ];

        if (HasHeaders && lines.Length > 1)
            return ToSelector(lines[1].Split(separator));

        return ToSelector(lines[0].Split(separator));
    }

    private Func<string, double> CommaConverter { get; } =
        s =>
            double.TryParse(
                s.Replace(".", "").Replace(",", "."),
                System.Globalization.NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d
            )
                ? d
                : double.NaN;

    private Func<string, double> DotConverter { get; } =
        s =>
            double.TryParse(
                s,
                System.Globalization.NumberStyles.Any,
                CultureInfo.InvariantCulture,
                out var d
            )
                ? d
                : double.NaN;
}
