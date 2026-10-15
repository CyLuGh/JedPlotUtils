using JDPlus.WS.Models;
using LanguageExt;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public readonly record struct TramoRequest(int NForecasts, Either<string, TramoSpec> Spec);

public partial class TramoForecastOptionsViewModel : BaseViewModel
{
    private readonly TaskCompletionSource<Option<TramoRequest>> _result = new();
    public Task<Option<TramoRequest>> Result => _result.Task;

    [Reactive]
    public partial int NForecasts { get; set; } = 1;

    public static string[] SimpleSpecs => ["TRfull", "TR0", "TR1", "TR2", "TR3", "TR4", "TR5"];

    [Reactive]
    public partial string SimpleSpec { get; set; } = SimpleSpecs[0];

    [Reactive]
    public partial bool UseSimpleSpec { get; set; } = true;

    [ReactiveCommand]
    public void Validate()
    {
        _result.TrySetResult(CreateRequest());
    }

    private Option<TramoRequest> CreateRequest()
    {
        if (UseSimpleSpec)
        {
            return new TramoRequest(NForecasts, SimpleSpec);
        }

        return new TramoRequest(NForecasts, new TramoSpec());
    }

    [ReactiveCommand]
    public void Cancel()
    {
        _result.TrySetResult(Option<TramoRequest>.None);
    }
}
