using JDPlus.WS.Models;
using LanguageExt;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public readonly record struct TramoRequest();

public partial class TramoForecastOptionsViewModel : BaseViewModel
{
    private readonly TaskCompletionSource<Option<TramoRequest>> _result = new();
    public Task<Option<TramoRequest>> Result => _result.Task;

    // [ReactiveCommand]
    // public void Validate()
    // {
    //     _result.TrySetResult(CreateRequest());
    // }

    [ReactiveCommand]
    public void Cancel()
    {
        _result.TrySetResult(Option<TramoRequest>.None);
    }
}
