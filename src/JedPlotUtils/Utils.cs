namespace JedPlotUtils;

public static class Utils
{
    public static double ToOADate(this DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToOADate();

    public static DateOnly ToDateOnly(this double oaDate) =>
        DateOnly.FromDateTime(DateTime.FromOADate(oaDate));
}
