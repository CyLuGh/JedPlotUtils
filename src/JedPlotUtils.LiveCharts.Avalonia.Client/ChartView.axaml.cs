using Avalonia.Controls;
using Huskui.Avalonia.Controls;
using Huskui.Avalonia.Models;
using JedPlotUtils.LiveCharts.Avalonia.Components.ViewModels;
using JedPlotUtils.Models;
using JedPlotUtils.ViewModels;
using ReactiveUI.Primitives;

namespace JedPlotUtils.LiveCharts.Avalonia.Client;

public partial class ChartView : UserControl
{
    private AppSurface? GetAppSurface() => AppSurface.GetAppSurface(this);

    public ChartView()
    {
        InitializeComponent();
        var tsvvm = new TimeSeriesViewerViewModel();
        RegisterNotifications(tsvvm);

        ChartViewHost.ViewModel = tsvvm;
    }

    private void RegisterNotifications(TimeSeriesViewerViewModel tsvvm)
    {
        tsvvm.CelebrateInteraction.RegisterHandler(async ctx =>
        {
            var item = ctx.Input.ToItem(GrowlLevel.Success);
            GetAppSurface()?.PopGrowl(item);
            await Task.Delay(10_000);
            item.Dismiss();
            ctx.SetOutput(RxVoid.Default);
        });

        tsvvm.InformInteraction.RegisterHandler(async ctx =>
        {
            var item = ctx.Input.ToItem(GrowlLevel.Information);
            GetAppSurface()?.PopGrowl(item);
            await Task.Delay(10_000);
            item.Dismiss();
            ctx.SetOutput(RxVoid.Default);
        });

        tsvvm.WarnInteraction.RegisterHandler(async ctx =>
        {
            var item = ctx.Input.ToItem(GrowlLevel.Warning);
            GetAppSurface()?.PopGrowl(item);
            await Task.Delay(30_000);
            item.Dismiss();
            ctx.SetOutput(RxVoid.Default);
        });

        tsvvm.PanicInteraction.RegisterHandler(ctx =>
        {
            var item = ctx.Input.ToItem(GrowlLevel.Danger);
            GetAppSurface()?.PopGrowl(item);
            ctx.SetOutput(RxVoid.Default);
        });
    }
}

public static class MessageExtensions
{
    public static GrowlItem ToItem(this Message message, GrowlLevel level) =>
        new()
        {
            Title = message.Title,
            Content = message.Content,
            Level = level,
        };
}
