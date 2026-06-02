using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Text;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using Avalonia.Xaml.Interactions.DragAndDrop;
using JedPlotUtils.ScottPlot.Common.Components.ViewModels;
using ReactiveUI;

namespace JedPlotUtils.Scottplot.Avalonia.Components
{
    public class SeriesDropHandler : DropHandlerBase
    {
        public override bool Validate(
            object? sender,
            DragEventArgs e,
            object? sourceContext,
            object? targetContext,
            object? state
        )
        {
            if (
                targetContext is TimeSeriesViewerViewModel
                && (
                    e.DataTransfer.Contains(DataFormat.File)
                    || e.DataTransfer.Contains(DataFormat.Text)
                )
            )
                return true;

            return false;
        }

        public override void Drop(
            object? sender,
            DragEventArgs e,
            object? sourceContext,
            object? targetContext
        )
        {
            if (targetContext is TimeSeriesViewerViewModel timeSeriesViewerViewModel)
            {
                var file = e.DataTransfer.TryGetFile();
                if (file is not null)
                {
                    Observable
                        .Return(file.TryGetLocalPath() ?? string.Empty)
                        .InvokeCommand(timeSeriesViewerViewModel, x => x.TryParseCsvCommand);
                }
                else
                {
                    var text = e.DataTransfer.TryGetText();
                    if (!string.IsNullOrEmpty(text)) { }
                }
            }

            e.Handled = true;
        }
    }
}
