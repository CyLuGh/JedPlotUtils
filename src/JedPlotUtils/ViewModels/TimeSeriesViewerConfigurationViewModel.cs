using System.Text.Json;
using JedPlotUtils.Models;
using JedPlotUtils.Palette;
using ReactiveUI;
using ReactiveUI.Primitives;
using ReactiveUI.SourceGenerators;
using Splat;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerConfigurationViewModel : BaseViewModel
{
    [Reactive]
    public partial string WebServiceAddress { get; set; } = string.Empty;

    [Reactive]
    public partial DisplayMode DisplayMode { get; set; }

    [Reactive]
    public partial IPalette? Palette { get; set; }

    [Reactive]
    public partial SelectionMode SeriesSelectionMode { get; set; }

    public static SelectionMode[] AvailableSeriesSelectionMode =>
        [SelectionMode.Single, SelectionMode.Multiple, SelectionMode.None];

    public static DisplayMode[] AvailableDisplayMode => Enum.GetValues<DisplayMode>();

    private readonly string _settingsPath = Path.Combine(
        Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
        System.Reflection.Assembly.GetEntryAssembly()?.GetName().Name ?? "unknown",
        "timeSeriesViewerSettings.json"
    );

    public RxCommand SaveCommand { get; }
    public RxCommand LoadCommand { get; }

    public TimeSeriesViewerConfigurationViewModel()
    {
        SaveCommand = CreateCommandSave();
        LoadCommand = CreateCommandLoad();

        RestoreSettings(Load());
    }

    private void RestoreSettings(TimeSeriesViewerSettings settings)
    {
        WebServiceAddress = !string.IsNullOrEmpty(settings.WebServiceAddress)
            ? settings.WebServiceAddress
            : TimeSeriesViewerSettings.DefaultWebServiceAddress;
        SeriesSelectionMode = settings.SeriesSelectionMode;
        DisplayMode = settings.DisplayMode;
        Palette = settings.Palette.ToPalette();
    }

    private RxCommand CreateCommandLoad()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(() =>
        {
            RestoreSettings(Load());
        });
        return cmd;
    }

    private RxCommand CreateCommandSave()
    {
        var cmd = ReactiveCommand.CreateRunInBackground(() =>
        {
            Save(
                new TimeSeriesViewerSettings()
                {
                    WebServiceAddress = WebServiceAddress,
                    SeriesSelectionMode = SeriesSelectionMode,
                    DisplayMode = DisplayMode,
                    Palette = Palette?.Name ?? string.Empty
                }
            );
        });
        return cmd;
    }

    public TimeSeriesViewerSettings Load()
    {
        try
        {
            if (!File.Exists(_settingsPath))
                return TimeSeriesViewerSettings.Default;

            var json = File.ReadAllText(_settingsPath);
            return JsonSerializer.Deserialize<TimeSeriesViewerSettings>(json)
                ?? TimeSeriesViewerSettings.Default;
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
            return TimeSeriesViewerSettings.Default;
        }
    }

    public void Save(TimeSeriesViewerSettings settings)
    {
        try
        {
            var directory = Path.GetDirectoryName(_settingsPath);
            if (!string.IsNullOrEmpty(directory) && !Directory.Exists(directory))
                Directory.CreateDirectory(directory);

            var json = JsonSerializer.Serialize(
                settings,
                new JsonSerializerOptions() { WriteIndented = true }
            );
            File.WriteAllText(_settingsPath, json);
        }
        catch (Exception ex)
        {
            this.Log().Error(ex);
        }
    }
}
