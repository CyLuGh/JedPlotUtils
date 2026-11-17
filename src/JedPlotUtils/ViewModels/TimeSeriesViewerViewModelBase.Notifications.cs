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

    public readonly record struct Message(string Title, string Content)
    {
        public Message(string content)
            : this(string.Empty, content) { }

        public Message(Exception exception)
            : this(exception.Message) { }

        public Message(string title, Exception exception)
            : this(title, exception.Message) { }

        public static implicit operator Message(string content) => new(content);

        public static implicit operator Message(Exception exception) => new(exception);
    }
}
