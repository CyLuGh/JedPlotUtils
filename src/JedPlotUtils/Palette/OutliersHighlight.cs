namespace JedPlotUtils.Palette;

public class OutliersHighlight(Color ao, Color tc, Color ls, Color so)
{
    public Color GetOutlierColor(string code) =>
        code switch
        {
            "AO" => ao,
            "TC" => tc,
            "LS" => ls,
            "SO" => so,
            _ => Color.FromRgb(255, 255, 255),
        };

    public Color GetOutlierContrastColor(string code) =>
        Color.GetBestForegroundColor(GetOutlierColor(code));
}
