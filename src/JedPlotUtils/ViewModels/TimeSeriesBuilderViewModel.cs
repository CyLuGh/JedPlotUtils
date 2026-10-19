using JDPlus.WS.Models;
using LanguageExt;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public readonly record struct TimeSeriesBuildOptions(
    Frequency Frequency,
    AggregationType AggregationType,
    bool RemoveOriginal
);

public partial class TimeSeriesBuilderViewModel : BaseViewModel
{
    private readonly TaskCompletionSource<Option<TimeSeriesBuildOptions>> _result = new();
    public Task<Option<TimeSeriesBuildOptions>> Result => _result.Task;

    public Frequency[] Frequencies => Enum.GetValues<Frequency>();
    public AggregationType[] AggregationTypes => Enum.GetValues<AggregationType>();

    [Reactive]
    public partial Frequency TargetFrequency { get; set; } = Frequency.Yearly;

    [Reactive]
    public partial AggregationType AggregationType { get; set; } = AggregationType.Sum;

    [Reactive]
    public partial bool RemoveOriginal { get; set; }

    [ReactiveCommand]
    public void Validate()
    {
        _result.TrySetResult(
            new TimeSeriesBuildOptions(TargetFrequency, AggregationType, RemoveOriginal)
        );
    }

    [ReactiveCommand]
    public void Cancel()
    {
        _result.TrySetResult(Option<TimeSeriesBuildOptions>.None);
    }
}
