using System.Text.Json;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using LanguageExt;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using Splat;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerConfigurationViewModel : BaseViewModel
{
    private readonly TaskCompletionSource<Option<TimeSeriesViewerSettings>> _result = new();
    public Task<Option<TimeSeriesViewerSettings>> Result => _result.Task;

    [Reactive]
    public partial string WebServiceAddress { get; set; } = string.Empty;

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }

    [Reactive]
    public partial IPalette? Palette { get; set; }

    [Reactive]
    public partial SelectionMode SeriesSelectionMode { get; set; }

    [Reactive]
    public partial string DateFormat { get; set; }

    [Reactive]
    public partial byte Decimals { get; set; }

    [Reactive]
    public partial bool HasThousandsSeparators { get; set; }

    [Reactive]
    public partial bool ShowStatusBar { get; set; }

    [Reactive]
    public partial bool SwitchChartAndGrid { get; set; }

    [Reactive]
    public partial bool AutoConnect { get; set; }

    [Reactive]
    public partial bool AutoScrollGrid { get; set; }

    [Reactive]
    public partial Color ColorAO { get; set; }

    [Reactive]
    public partial Color ColorLS { get; set; }

    [Reactive]
    public partial Color ColorTC { get; set; }

    [Reactive]
    public partial Color ColorSO { get; set; }

    [Reactive]
    public partial LegendDisplay Legend { get; set; }

    public static SelectionMode[] AvailableSeriesSelectionMode =>
        [SelectionMode.Single, SelectionMode.Multiple, SelectionMode.None];

    public static DisplayMode[] AvailableDisplayMode =>
        [DisplayMode.Chart, DisplayMode.Grid, DisplayMode.BothHorizontal, DisplayMode.BothVertical];

    public static LegendDisplay[] AvailableLegendDisplay => Enum.GetValues<LegendDisplay>();

    private static readonly string SettingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown",
        "timeSeriesViewerSettings.json"
    );

    [Reactive]
    public partial bool PersistSettings { get; set; }

    public ReactiveCommand<bool, RxVoid> SaveCommand { get; }
    public RxCommand CancelCommand { get; }
    public RxCommand LoadCommand { get; }

    public bool EnableDemetra { get; }
    public bool Restricted { get; }

    public TimeSeriesViewerConfigurationViewModel(
        Option<TimeSeriesViewerSettings> settings,
        bool enableDemetra,
        bool restricted
    )
    {
        EnableDemetra = enableDemetra;
        Restricted = restricted;

        SaveCommand = CreateCommandSave();
        CancelCommand = CreateCommandCancel();
        LoadCommand = CreateCommandLoad();

        DateFormat = string.Empty;
        RestoreSettings(
            settings.Match(
                s => s,
                () => Load().Match(s => s, () => TimeSeriesViewerSettings.Default)
            )
        );
    }

    private void RestoreSettings(TimeSeriesViewerSettings settings)
    {
        WebServiceAddress = !string.IsNullOrEmpty(settings.WebServiceAddress)
            ? settings.WebServiceAddress
            : TimeSeriesViewerSettings.DefaultWebServiceAddress;
        SeriesSelectionMode = settings.SeriesSelectionMode;
        DisplayMode = settings.DisplayMode;
        Palette = settings.Palette.ToPalette();
        DateFormat = settings.DateFormat ?? string.Empty;
        Decimals = settings.Decimals;
        HasThousandsSeparators = settings.HasThousandsSeparators;
        ShowStatusBar = settings.ShowStatusBar;
        SwitchChartAndGrid = settings.SwitchGridAndChart;
        AutoConnect = settings.AutoConnect;
        AutoScrollGrid = settings.AutoScrollGrid;
        ColorAO = settings.ColorAO;
        ColorLS = settings.ColorLS;
        ColorSO = settings.ColorSO;
        ColorTC = settings.ColorTC;
    }

    private RxCommand CreateCommandCancel() =>
        ReactiveCommand.Create(() =>
        {
            _result.TrySetResult(Option<TimeSeriesViewerSettings>.None);
        });

    private RxCommand CreateCommandLoad()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(() =>
        {
            RestoreSettings(Load().Match(s => s, () => TimeSeriesViewerSettings.Default));
        });
        return cmd;
    }

    private ReactiveCommand<bool, RxVoid> CreateCommandSave()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(
            (bool save) =>
            {
                var settings = new TimeSeriesViewerSettings()
                {
                    WebServiceAddress = WebServiceAddress,
                    SeriesSelectionMode = SeriesSelectionMode,
                    DisplayMode = DisplayMode,
                    Palette = Palette?.Name ?? string.Empty,
                    DateFormat = DateFormat,
                    Decimals = Decimals,
                    HasThousandsSeparators = HasThousandsSeparators,
                    ShowStatusBar = ShowStatusBar,
                    SwitchGridAndChart = SwitchChartAndGrid,
                    AutoConnect = AutoConnect,
                    AutoScrollGrid = AutoScrollGrid,
                    ColorAO = ColorAO,
                    ColorLS = ColorLS,
                    ColorSO = ColorSO,
                    ColorTC = ColorTC,
                    Legend = Legend,
                };

                if (save)
                    Save(settings);

                _result.TrySetResult(settings);
            }
        );
        return cmd;
    }

    public static Option<TimeSeriesViewerSettings> Load()
    {
        try
        {
            if (!File.Exists(SettingsPath))
                return Option<TimeSeriesViewerSettings>.None;

            var json = File.ReadAllText(SettingsPath);
            return JsonSerializer.Deserialize<TimeSeriesViewerSettings>(json)
                ?? Option<TimeSeriesViewerSettings>.None;
        }
        catch (Exception)
        {
            return Option<TimeSeriesViewerSettings>.None;
        }
    }

    private void Save(TimeSeriesViewerSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(SettingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions() { WriteIndented = true }
            );
            File.WriteAllText(SettingsPath, json);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }
}
