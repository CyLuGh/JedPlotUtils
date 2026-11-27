using System.Globalization;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Platform.Storage;
using JedPlotUtils.Avalonia.Components;
using JedPlotUtils.LiveCharts.ViewModels;
using JedPlotUtils.Models;
using JedPlotUtils.ViewModels;
using LanguageExt;
using LiveChartsCore.Defaults;
using LiveChartsCore.Drawing;
using LiveChartsCore.Kernel;
using LiveChartsCore.Kernel.Events;
using LiveChartsCore.Kernel.Sketches;
using LiveChartsCore.SkiaSharpView.Avalonia;
using ReactiveUI;
using ReactiveUI.Avalonia;
using ReactiveUI.Primitives;
using ReactiveUI.Primitives.Disposables;
using ReactiveUI.Primitives.Extensions;
using ReactiveUI.Primitives.Signals;
using AV = Avalonia;
using SelectionMode = JedPlotUtils.Models.SelectionMode;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public partial class TimeSeriesViewer : ReactiveUserControl<TimeSeriesViewerViewModel>
{
    public static readonly AV.StyledProperty<double> RowHeaderWidthProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, double>(
            nameof(RowHeaderWidth),
            defaultValue: 100
        );

    public double RowHeaderWidth
    {
        get => GetValue(RowHeaderWidthProperty);
        set => SetValue(RowHeaderWidthProperty, value);
    }

    public static readonly AV.StyledProperty<bool> ShowStatusBarProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, bool>(
            nameof(ShowStatusBar),
            defaultValue: true
        );

    public bool ShowStatusBar
    {
        get => GetValue(ShowStatusBarProperty);
        set => SetValue(ShowStatusBarProperty, value);
    }

    public static readonly AV.StyledProperty<bool> EnableDemetraProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, bool>(
            nameof(EnableDemetra),
            defaultValue: true
        );

    public bool EnableDemetra
    {
        get => GetValue(EnableDemetraProperty);
        set => SetValue(EnableDemetraProperty, value);
    }

    public static readonly AV.StyledProperty<bool> RestrictedProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, bool>(
            nameof(Restricted),
            defaultValue: false
        );

    public bool Restricted
    {
        get => GetValue(RestrictedProperty);
        set => SetValue(RestrictedProperty, value);
    }

    public static readonly AV.StyledProperty<JedPlotUtils.Models.DisplayMode> DisplayModeProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, JedPlotUtils.Models.DisplayMode>(
            nameof(DisplayMode),
            defaultValue: JedPlotUtils.Models.DisplayMode.BothHorizontal
        );

    public JedPlotUtils.Models.DisplayMode DisplayMode
    {
        get => GetValue(DisplayModeProperty);
        set => SetValue(DisplayModeProperty, value);
    }

    public static readonly AV.StyledProperty<bool> HasThousandsSeparatorProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, bool>(
            nameof(HasThousandsSeparator),
            defaultValue: true
        );

    public bool HasThousandsSeparator
    {
        get => GetValue(HasThousandsSeparatorProperty);
        set => SetValue(HasThousandsSeparatorProperty, value);
    }

    public static readonly AV.StyledProperty<byte> DecimalsProperty = AV.AvaloniaProperty.Register<
        TimeSeriesViewer,
        byte
    >(nameof(Decimals), defaultValue: 0);

    public byte Decimals
    {
        get => GetValue(DecimalsProperty);
        set => SetValue(DecimalsProperty, value);
    }

    public static readonly AV.StyledProperty<LegendDisplay> LegendDisplayProperty =
        AV.AvaloniaProperty.Register<TimeSeriesViewer, LegendDisplay>(
            nameof(LegendDisplay),
            defaultValue: LegendDisplay.Hidden
        );

    public LegendDisplay LegendDisplay
    {
        get => GetValue(LegendDisplayProperty);
        set => SetValue(LegendDisplayProperty, value);
    }

    public TimeSeriesViewer()
    {
        InitializeComponent();

        this.WhenActivated(disposables =>
        {
            this.WhenAnyValue(x => x.ViewModel)
                .Where(vm => vm is not null)
                .Select(vm => vm!)
                .Do(vm => PopulateFromViewModel(this, vm, disposables))
                .Subscribe()
                .DisposeWith(disposables);
        });
    }

    private async Task OpenDialogAnimation(
        TimeSeriesViewerViewModel viewModel,
        object? content,
        double width = 600,
        double height = 400
    )
    {
        DialogContent.Content = content;
        viewModel.IsDialogOpen = true;
        await Task.Delay(50);

        viewModel.DialogOpacity = 1;
        DialogBorder.Width = width;
        DialogBorder.Height = height;
    }

    private async Task CloseDialogAnimation(TimeSeriesViewerViewModel viewModel)
    {
        DialogBorder.Width = 100;
        DialogBorder.Height = 50;
        viewModel.DialogOpacity = 0;
        await Task.Delay(220);
        viewModel.IsDialogOpen = false;
        DialogContent.Content = null;
    }

    private Option<PointerPressedEventArgs> _dragPointerPressedEventArgs;
    private bool _dragStarted;

    private void PopulateFromViewModel(
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        view.HierarchyGrid.ViewModel = viewModel.HierarchyGridViewModel;

        ApplyStyledProperties(view, viewModel);

        Signal
            .FromEventPattern<KeyEventArgs>(
                handler => view.KeyDown += handler,
                handler => view.KeyDown -= handler
            )
            .SubscribePrimitives(t =>
            {
                var args = t.EventArgs;
                if (args.Key is Key.LeftAlt or Key.RightAlt)
                {
                    viewModel.TempShowStatusBar = true;
                    args.Handled = true;
                }
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<KeyEventArgs>(
                handler => view.KeyUp += handler,
                handler => view.KeyUp -= handler
            )
            .SubscribePrimitives(t =>
            {
                viewModel.TempShowStatusBar = false;
            })
            .DisposeWith(disposables);

        viewModel.ClearInfoInteraction.RegisterHandler(ctx =>
        {
            view.InfoContent.Content = null;
            ctx.SetOutput(RxVoid.Default);
        });

        viewModel.ShowDisaggregationInfoInteraction.RegisterHandler(ctx =>
        {
            view.InfoContent.Content = new DisaggregationResultsInfoView()
            {
                DataContext = ctx.Input,
            };
            ctx.SetOutput(RxVoid.Default);
        });

        viewModel
            .ShowSettingsInteraction.RegisterHandler(async ctx =>
            {
                var (settings, enableDemetra, restricted) = ctx.Input;
                var cvm = new TimeSeriesViewerConfigurationViewModel(
                    settings,
                    enableDemetra,
                    restricted
                );
                await OpenDialogAnimation(
                    viewModel,
                    new TimeSeriesViewerConfigurationView() { ViewModel = cvm },
                    700,
                    400
                );
                var res = await cvm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        viewModel
            .ShowConnectionSettingsInteraction.RegisterHandler(async ctx =>
            {
                var (settings, enableDemetra, restricted) = ctx.Input;
                var cvm = new TimeSeriesViewerConfigurationViewModel(
                    settings,
                    enableDemetra,
                    restricted
                );
                await OpenDialogAnimation(
                    viewModel,
                    new TimeSeriesViewerConnectionView() { ViewModel = cvm },
                    520,
                    220
                );
                var res = await cvm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        viewModel
            .GetDisaggregationRequestInteraction.RegisterHandler(async ctx =>
            {
                var dovm = new DisaggregationOptionsViewModel();
                await OpenDialogAnimation(
                    viewModel,
                    new DisaggregationOptionsView() { ViewModel = dovm },
                    700,
                    700
                );
                var res = await dovm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        viewModel
            .GetTramoRequestInteraction.RegisterHandler(async ctx =>
            {
                var tfovm = new TramoForecastOptionsViewModel();
                await OpenDialogAnimation(
                    viewModel,
                    new TramoForecastOptionsView() { ViewModel = tfovm },
                    700,
                    700
                );
                var res = await tfovm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        viewModel
            .RenameSeriesInteraction.RegisterHandler(async ctx =>
            {
                var rsvm = new RenameSeriesViewModel() { CurrentName = ctx.Input.Label };
                await OpenDialogAnimation(
                    viewModel,
                    new RenameSeriesView() { ViewModel = rsvm },
                    450,
                    280
                );
                var res = await rsvm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        viewModel
            .GetFrequencyChangeOptionsInteraction.RegisterHandler(async ctx =>
            {
                var tsbvm = new TimeSeriesBuilderViewModel();
                await OpenDialogAnimation(
                    viewModel,
                    new TimeSeriesBuilderView() { ViewModel = tsbvm },
                    450,
                    320
                );
                var res = await tsbvm.Result;
                ctx.SetOutput(res);
                await CloseDialogAnimation(viewModel);
            })
            .DisposeWith(disposables);

        /* Display Mode */
        viewModel
            .AdaptLayoutInteraction.RegisterHandler(ctx =>
            {
                var (mode, switchPositions) = ctx.Input;
#if DEBUG
                Console.WriteLine($"Calling AdaptDisplayModeInteraction with {ctx.Input}");
#endif
                SetHierarchyGridConstraints(
                    view.HierarchyGrid,
                    mode,
                    viewModel.HierarchyGridViewModel,
                    switchPositions
                );
                SetChartConstraints(view.CartesianChart, mode, switchPositions);
                SetGridSplitterConstraints(view.Splitter, mode);
                SetGridDefinitions(view.GridMainDisplay, mode);
                viewModel.HasFirstRender = true;
                ctx.SetOutput(RxVoid.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .CopyToClipboardInteraction.RegisterHandler(async ctx =>
            {
                var clipboard = TopLevel.GetTopLevel(view)?.Clipboard;

                if (clipboard is not null)
                {
                    using var dataTransfer = new DataTransfer();
                    dataTransfer.Add(
                        DataTransferItem.Create(
                            DataFormat.CreateBytesApplicationFormat("jedplot.timeseries"),
                            viewModel.SelectedSeries.ToBytes()
                        )
                    );

                    await clipboard.SetDataAsync(dataTransfer);

                    var xmlss = viewModel.SelectedSeries.ToXmlSpreadSheet();
                    dataTransfer.Add(
                        DataTransferItem.Create(
                            DataFormat.CreateStringPlatformFormat("application/vnd.ms-excel"),
                            xmlss
                        )
                    );
                }

                ctx.SetOutput(RxVoid.Default);
            })
            .DisposeWith(disposables);

        viewModel
            .PasteFromClipboardInteraction.RegisterHandler(async ctx =>
            {
                var clipboard = TopLevel.GetTopLevel(view)?.Clipboard;

                if (clipboard is null)
                {
                    ctx.SetOutput(Seq<TimeSeriesInfo>.Empty);
                    return;
                }

                using var data = await clipboard.TryGetDataAsync().ConfigureAwait(false);

                var bytes =
                    await data?.TryGetValueAsync(
                        DataFormat.CreateBytesApplicationFormat("jedplot.timeseries")
                    )! ?? [];

                if (bytes.Length > 0)
                {
                    ctx.SetOutput(bytes.ToTimeSeriesInfo().ToSeq());
                    return;
                }

                /* Text format */
                var text = await data?.TryGetTextAsync()! ?? string.Empty;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var series = text.FromCsv();
                    if (series.Length > 0)
                    {
                        ctx.SetOutput(series.ToSeq());
                        return;
                    }
                }

                /* File format */
                if (await data?.TryGetFilesAsync()! is { } files)
                {
                    // TODO: check csv, check xlsx, check OpenOffice
                    await ParseIStorageItems(files, viewModel);
                }

                ctx.SetOutput(Seq<TimeSeriesInfo>.Empty);
            })
            .DisposeWith(disposables);

        HandleDragDrop(view, viewModel, disposables);

        /* Mouse click */
        Signal
            .FromEventPattern<EventHandler<PointerPressedEventArgs>, PointerPressedEventArgs>(
                handler => view.CartesianChart.PointerPressed += handler,
                handler => view.CartesianChart.PointerPressed -= handler
            )
            .Subscribe(t =>
            {
                var args = t.EventArgs;

                /* Only update selection from left click (except when selection is empty) and if selection mode allows it */
                if (
                    viewModel.SeriesSelectionMode == SelectionMode.None
                    || (!args.Properties.IsLeftButtonPressed && !viewModel.Selection.IsEmpty)
                )
                    return;

                var chart = (CartesianChart)t.Sender!;
                var pos = args.GetPosition(chart);

                // Convert Avalonia point → LiveCharts point
                var lvcPoint = new LvcPoint((float)pos.X, (float)pos.Y);

                // Hit test
                var found = chart.CoreChart.FindHoveredPointsBy(lvcPoint).ToSeq();

                if (found.IsEmpty)
                {
                    viewModel.Selection = LanguageExt.HashSet<Identifier>.Empty;
                    return;
                }

                viewModel.Selection = viewModel.SeriesSelectionMode switch
                {
                    SelectionMode.Single => viewModel
                        .Selection.Clear()
                        .Add((Identifier)found[0].Context.Series.Tag!),
                    SelectionMode.Multiple => viewModel.Selection.TryAdd(
                        (Identifier)found[0].Context.Series.Tag!
                    ),
                    _ => viewModel.Selection,
                };

                if (!viewModel.Selection.IsEmpty && args.Properties.IsLeftButtonPressed)
                {
                    _dragPointerPressedEventArgs = args;
                    _dragStarted = false;
                }
            })
            .DisposeWith(disposables);

        /* Mouse over chart */
        Signal
            .FromEvent<
                ChartPointHoverHandler,
                (
                    IChartView chart,
                    IEnumerable<ChartPoint>? newItems,
                    IEnumerable<ChartPoint>? oldItems
                )
            >(
                handler => (chart, newItems, oldItems) => handler((chart, newItems, oldItems)),
                h => view.CartesianChart.HoveredPointsChanged += h,
                h => view.CartesianChart.HoveredPointsChanged -= h
            )
            .Throttle(TimeSpan.FromMilliseconds(20))
            .Subscribe(x =>
            {
                var (_, newItems, _) = x;
                var nItems = newItems?.ToSeq() ?? Seq<ChartPoint>.Empty;
                if (!nItems.IsEmpty)
                {
                    var point = nItems[0];
                    if (
                        point.Context is
                        { DataSource: DateTimePoint dtp, Series.Tag: Identifier identifier }
                    )
                    {
                        viewModel.HoveredPoint = (identifier, DateOnly.FromDateTime(dtp.DateTime));
                    }
                    else
                    {
                        viewModel.HoveredPoint = Option<(Identifier, DateOnly)>.None;
                    }
                }
                else
                {
                    viewModel.HoveredPoint = Option<(Identifier, DateOnly)>.None;
                }
            })
            .DisposeWith(disposables);

        viewModel.HighlightChartPointInteraction.RegisterHandler(ctx =>
        {
            ctx.Input.IfSome(t =>
            {
                var (identifier, period) = t;
                var dateTime = period.ToDateTime(TimeOnly.MinValue);

                var chartPoints = (
                    from s in view.CartesianChart.Series.Find(s =>
                        s.Tag?.Equals(identifier) == true
                    )
                    from pt in s.Fetch(view.CartesianChart.CoreChart)
                        .Where(p =>
                            p.Context.DataSource is DateTimePoint op && op.DateTime == dateTime
                        )
                    select pt
                ).ToSeq();

                /* Don't show tooltip when hovering any empty cell */
                if (!chartPoints.IsEmpty && !chartPoints[0].IsEmpty)
                    view.CartesianChart.Tooltip?.Show(chartPoints, view.CartesianChart.CoreChart);
                else
                    view.CartesianChart.Tooltip?.Hide(view.CartesianChart.CoreChart);
            });

            ctx.Input.IfNone(() =>
            {
                view.CartesianChart.Tooltip?.Hide(view.CartesianChart.CoreChart);
            });

            ctx.SetOutput(RxVoid.Default);
        });

        viewModel.IsInitialized = true;
    }

    private static void ApplyStyledProperties(
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel
    )
    {
        viewModel.EnableDemetra = view.EnableDemetra;
        viewModel.ShowStatusBar = view.ShowStatusBar;
        viewModel.Restricted = view.Restricted;
        Signal.Return(view.DisplayMode).InvokeCommand(viewModel, x => x.ChangeDisplayModeCommand);
        Signal
            .Return(view.HasThousandsSeparator)
            .InvokeCommand(viewModel, x => x.ChangeThousandsSeparatorCommand);
        Signal.Return(view.Decimals).InvokeCommand(viewModel, x => x.ChangeDecimalsCommand);
        Signal
            .Return(view.LegendDisplay)
            .InvokeCommand(viewModel, x => x.ChangeLegendDisplayCommand);
    }

    private void HandleDragDrop(
        TimeSeriesViewer view,
        TimeSeriesViewerViewModel viewModel,
        MultipleDisposable disposables
    )
    {
        Signal
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler => view.CartesianChart.PointerReleased += handler,
                handler => view.CartesianChart.PointerReleased -= handler
            )
            .Subscribe(_ =>
            {
                _dragStarted = false;
                _dragPointerPressedEventArgs = Option<PointerPressedEventArgs>.None;
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<PointerEventArgs>, PointerEventArgs>(
                handler => view.CartesianChart.PointerMoved += handler,
                handler => view.CartesianChart.PointerMoved -= handler
            )
            .SubscribeAsync(async t =>
            {
                var args = t.EventArgs;
                if (
                    args.Properties.IsLeftButtonPressed
                    && !viewModel.Selection.IsEmpty
                    && !_dragStarted
                )
                {
                    await _dragPointerPressedEventArgs.IfSomeAsync(async evt =>
                    {
                        var delta = args.GetPosition(view) - evt.GetPosition(view);
                        if (delta is { X: < 5, Y: < 5 })
                            return;

                        _dragStarted = true;

                        var dragData = new DataTransfer();

                        string filePath;

                        if ((args.KeyModifiers & KeyModifiers.Control) != 0)
                        {
                            var csv = viewModel.SelectedSeries.ToCsv(
                                separator: "\t",
                                formatProvider: CultureInfo.CurrentCulture
                            );
                            dragData.Add(DataTransferItem.CreateText(csv));

                            filePath = Path.Combine(
                                Path.GetTempPath(),
                                $"{Guid.CreateVersion7():N}.xlsx"
                            );
                            await File.WriteAllBytesAsync(
                                filePath,
                                viewModel.SelectedSeries.ToExcelBytes()
                            );
                        }
                        else
                        {
                            var csv = viewModel.SelectedSeries.ToCsv();
                            dragData.Add(DataTransferItem.CreateText(csv));

                            filePath = Path.Combine(
                                Path.GetTempPath(),
                                $"{Guid.CreateVersion7():N}.csv"
                            );
                            await File.WriteAllTextAsync(filePath, csv);
                        }

                        var storageFile = await (
                            TopLevel
                                .GetTopLevel(view)
                                ?.StorageProvider.TryGetFileFromPathAsync(filePath)!
                        ).ConfigureAwait(false);
                        if (storageFile is not null)
                            dragData.Add(DataTransferItem.CreateFile(storageFile));

                        dragData.Add(
                            DataTransferItem.Create(
                                DataFormat.CreateBytesApplicationFormat("jedplot.timeseries"),
                                viewModel.SelectedSeries.ToBytes()
                            )
                        );

                        await DragDrop.DoDragDropAsync(evt, dragData, DragDropEffects.Copy);

                        _dragStarted = false;
                        _dragPointerPressedEventArgs = Option<PointerPressedEventArgs>.None;
                    });
                }
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<DragEventArgs>, DragEventArgs>(
                handler => DragDrop.AddDragOverHandler(view.GridMainDisplay, handler),
                handler => DragDrop.AddDragOverHandler(view.GridMainDisplay, handler)
            )
            .Subscribe(t =>
            {
                var args = t.EventArgs;
                args.DragEffects =
                    args.DataTransfer.Formats.Contains(DataFormat.Text)
                    || args.DataTransfer.Formats.Contains(DataFormat.File)
                        ? DragDropEffects.Copy
                        : DragDropEffects.None;
            })
            .DisposeWith(disposables);

        Signal
            .FromEventPattern<EventHandler<DragEventArgs>, DragEventArgs>(
                handler => DragDrop.AddDropHandler(view.GridMainDisplay, handler),
                handler => DragDrop.RemoveDropHandler(view.GridMainDisplay, handler)
            )
            .SubscribeAsync(async t =>
            {
                var e = t.EventArgs;

                /* Internal binary format */
                var bytes = e.DataTransfer.TryGetValue(
                    DataFormat.CreateBytesApplicationFormat("jedplot.timeseries")
                );

                if (bytes?.Length > 0)
                {
                    viewModel.Add(bytes.ToTimeSeriesInfo());
                    return;
                }

                /* Text format */
                var text = e.DataTransfer.TryGetText();
                if (!string.IsNullOrWhiteSpace(text))
                {
                    var series = text.FromCsv();
                    if (series.Length > 0)
                    {
                        viewModel.Add(series);
                        return;
                    }
                }

                /* File format */
                if (e.DataTransfer.TryGetFiles() is { } files)
                {
                    await ParseIStorageItems(files, viewModel);
                }
            })
            .DisposeWith(disposables);
    }

    private async Task ParseIStorageItems(IStorageItem[] files, TimeSeriesViewerViewModel viewModel)
    {
        // TODO: check OpenOffice
        var stackPanel = new StackPanel()
        {
            Orientation = Orientation.Vertical,
            Spacing = 10d,
            VerticalAlignment = VerticalAlignment.Center,
            Children =
            {
                new TextBlock()
                {
                    Text = "Parsing file content...",
                    VerticalAlignment = VerticalAlignment.Center,
                    HorizontalAlignment = HorizontalAlignment.Center,
                    FontSize = 18,
                },
                new ProgressBar()
                {
                    IsIndeterminate = true,
                    Height = 15,
                    Width = 250,
                    VerticalAlignment = VerticalAlignment.Center,
                },
            },
        };

        await OpenDialogAnimation(viewModel, stackPanel, 450, 320);

        await Task.Run(() =>
        {
            foreach (var file in files)
            {
                var extension = Path.GetExtension(file.Path.LocalPath).ToLowerInvariant();
                switch (extension)
                {
                    case ".txt":
                    case ".csv":
                        {
                            var series = File.ReadAllText(file.Path.LocalPath).FromCsv();
                            if (series.Length > 0)
                                viewModel.Add(series);
                        }
                        break;

                    case ".xlsx":
                        {
                            var series = ExcelTimeSeriesInfoParser.FromExcel(file.Path.LocalPath);
                            if (series.Length > 0)
                                viewModel.Add(series);
                        }
                        break;
                }
            }
        });

        await CloseDialogAnimation(viewModel);
    }

    private static void SetGridSplitterConstraints(GridSplitter splitter, DisplayMode displayMode)
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, 1);

                splitter.VerticalAlignment = AV.Layout.VerticalAlignment.Center;
                splitter.HorizontalAlignment = AV.Layout.HorizontalAlignment.Stretch;

                splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                splitter.ResizeDirection = GridResizeDirection.Rows;

                break;

            case DisplayMode.BothHorizontal:
                Grid.SetColumn(splitter, 1);
                Grid.SetRow(splitter, 0);

                splitter.VerticalAlignment = AV.Layout.VerticalAlignment.Stretch;
                splitter.HorizontalAlignment = AV.Layout.HorizontalAlignment.Center;

                splitter.ResizeBehavior = GridResizeBehavior.PreviousAndNext;
                splitter.ResizeDirection = GridResizeDirection.Columns;
                break;

            default:
                Grid.SetColumn(splitter, 0);
                Grid.SetRow(splitter, 0);
                break;
        }
    }

    private static void SetChartConstraints(
        CartesianChart chart,
        DisplayMode displayMode,
        bool switchPositions
    )
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                Grid.SetColumn(chart, 0);
                Grid.SetRow(chart, !switchPositions ? 0 : 2);
                break;

            case DisplayMode.BothHorizontal:
                Grid.SetColumn(chart, !switchPositions ? 0 : 2);
                Grid.SetRow(chart, 0);
                break;

            default:
                Grid.SetColumn(chart, 0);
                Grid.SetRow(chart, 0);
                break;
        }
    }

    private static void SetHierarchyGridConstraints(
        HierarchyGrid.Avalonia.Grid hierarchyGrid,
        DisplayMode displayMode,
        HierarchyGrid.Definitions.HierarchyGridViewModel hierarchyGridViewModel,
        bool switchPositions
    )
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                Grid.SetColumn(hierarchyGrid, 0);
                Grid.SetRow(hierarchyGrid, !switchPositions ? 2 : 0);
                hierarchyGridViewModel.IsTransposed = false;
                break;

            case DisplayMode.BothHorizontal:
                Grid.SetColumn(hierarchyGrid, !switchPositions ? 2 : 0);
                Grid.SetRow(hierarchyGrid, 0);
                hierarchyGridViewModel.IsTransposed = true;
                break;

            default:
                Grid.SetColumn(hierarchyGrid, 0);
                Grid.SetRow(hierarchyGrid, 0);
                hierarchyGridViewModel.IsTransposed = true;
                break;
        }
    }

    private static void SetGridDefinitions(Grid grid, DisplayMode displayMode)
    {
        switch (displayMode)
        {
            case DisplayMode.BothVertical:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*");
                grid.RowDefinitions = RowDefinitions.Parse("2*,Auto,*");
                break;

            case DisplayMode.BothHorizontal:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("2*,Auto,*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;

            case DisplayMode.None:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("0,0,0");
                grid.RowDefinitions = RowDefinitions.Parse("0,0,0");
                break;

            default:
                grid.ColumnDefinitions = ColumnDefinitions.Parse("*");
                grid.RowDefinitions = RowDefinitions.Parse("*");
                break;
        }
    }
}
