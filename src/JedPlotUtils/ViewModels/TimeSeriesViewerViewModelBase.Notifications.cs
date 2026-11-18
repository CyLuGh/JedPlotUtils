using JedPlotUtils.Models;
using ReactiveUI;
using ReactiveUI.Primitives;

namespace JedPlotUtils.ViewModels;

public partial class TimeSeriesViewerViewModelBase
{
    public ReactiveCommand<Message, RxVoid> Celebrate { get; }
    public ReactiveCommand<Message, RxVoid> Inform { get; }
    public ReactiveCommand<Message, RxVoid> Warn { get; }
    public ReactiveCommand<Message, RxVoid> Panic { get; }

    public Interaction<Message, RxVoid> CelebrateInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public Interaction<Message, RxVoid> InformInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public Interaction<Message, RxVoid> WarnInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
    public Interaction<Message, RxVoid> PanicInteraction { get; } =
        new(RxSchedulers.MainThreadScheduler);
}
