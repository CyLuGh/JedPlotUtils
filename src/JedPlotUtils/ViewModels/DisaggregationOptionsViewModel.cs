using JDPlus.WS.Models;
using LanguageExt;
using ReactiveUI.SourceGenerators;

namespace JedPlotUtils.ViewModels;

public partial class DisaggregationOptionsViewModel : BaseViewModel
{
    private readonly TaskCompletionSource<Option<TemporalDisaggregationRequest>> _result = new();
    public Task<Option<TemporalDisaggregationRequest>> Result => _result.Task;

    [Reactive]
    public partial string Algorithm { get; set; } = "SqrtDiffuse";

    [Reactive]
    public partial string Model { get; set; } = "Rw";

    [Reactive]
    public partial bool Average { get; set; }

    [Reactive]
    public partial bool Constant { get; set; }

    [Reactive]
    public partial bool DiffuserEgs { get; set; }

    [Reactive]
    public partial bool FixedRho { get; set; }

    [Reactive]
    public partial bool Trend { get; set; }

    [Reactive]
    public partial bool ZeroInit { get; set; }

    [Reactive]
    public partial double Rho { get; set; }

    [Reactive]
    public partial double TruncatedRho { get; set; }

    [Reactive]
    public partial bool HasFrequency { get; set; }

    [Reactive]
    public partial Frequency Frequency { get; set; }

    [Reactive]
    public partial bool HasForecast { get; set; }

    [Reactive]
    public partial int NForecasts { get; set; }

    [Reactive]
    public partial bool HasBackcast { get; set; }

    [Reactive]
    public partial int NBackcasts { get; set; }

    public Frequency[] Frequencies => Enum.GetValues<Frequency>();

    [ReactiveCommand]
    public void Validate()
    {
        _result.TrySetResult(CreateRequest());
    }

    [ReactiveCommand]
    public void Cancel()
    {
        _result.TrySetResult(Option<TemporalDisaggregationRequest>.None);
    }

    public TemporalDisaggregationRequest CreateRequest() =>
        new()
        {
            Algorithm = Algorithm,
            Model = Model,
            Average = Average,
            Constant = Constant,
            DiffuserEgs = DiffuserEgs,
            FixedRho = FixedRho,
            Trend = Trend,
            ZeroInit = ZeroInit,
            Rho = Rho,
            TruncatedRho = TruncatedRho,
            Frequency = HasFrequency ? (int)Frequency : Option<int>.None,
            NForecasts = HasForecast ? NForecasts : Option<int>.None,
            NBackcasts = HasBackcast ? NBackcasts : Option<int>.None,
        };
}
