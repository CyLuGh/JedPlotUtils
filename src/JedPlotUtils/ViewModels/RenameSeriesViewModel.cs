using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public partial class RenameSeriesViewModel : BaseViewModel
{
    public required string? CurrentName { get; init; }

    [Reactive]
    public partial string? NewName { get; set; }

    private readonly TaskCompletionSource<string?> _result = new();
    public Task<string?> Result => _result.Task;

    [ReactiveCommand]
    public void Validate()
    {
        _result.TrySetResult(NewName);
    }

    [ReactiveCommand]
    public void Cancel()
    {
        _result.TrySetResult(string.Empty);
    }
}
