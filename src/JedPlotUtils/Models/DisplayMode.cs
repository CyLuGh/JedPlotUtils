using System.ComponentModel;

namespace JedPlotUtils.Models;

public enum DisplayMode
{
    None,

    [Description("Chart only")]
    Chart,

    [Description("Grid only")]
    Grid,

    [Description("Chart and grid on horizontal display")]
    BothHorizontal,

    [Description("Chart and grid on vertical display")]
    BothVertical,
}
