using System.ComponentModel;
using System.Runtime.CompilerServices;
using LiveChartsCore.Kernel;

namespace JedPlotUtils.LiveCharts.Avalonia.Components;

public class DateTimeRangeValue : IChartEntity, INotifyPropertyChanged
{
    public DateTimeRangeValue() { }

    public DateTimeRangeValue(DateTime dateTime, double? low, double? high)
    {
        // MetaData = new ChartEntityMetaData(OnCoordinateChanged);
        DateTime = dateTime;
        Low = low;
        High = high;
        Coordinate =
            low is null || high is null
                ? Coordinate.Empty
                : new(high.Value, dateTime.Ticks, low.Value, 0, 0, 0, Error.Empty);
    }

    public ChartEntityMetaData? MetaData { get; set; }
    public Coordinate Coordinate { get; set; } = Coordinate.Empty;

    public DateTime DateTime
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public double? Low
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    /// <summary>
    /// Gets or sets the high endpoint of the range. Null marks the point as empty.
    /// </summary>
    public double? High
    {
        get;
        set
        {
            field = value;
            OnPropertyChanged();
        }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        Coordinate =
            Low is null || High is null
                ? Coordinate.Empty
                : new(High.Value, DateTime.Ticks, Low.Value, 0, 0, 0, Error.Empty);
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}
