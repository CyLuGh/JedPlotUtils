using ReactiveUI;

namespace JedPlotUtils.ViewModels;

public abstract class BaseViewModel : ReactiveObject, IActivatableViewModel
{
    public ViewModelActivator Activator { get; } = new();
}
