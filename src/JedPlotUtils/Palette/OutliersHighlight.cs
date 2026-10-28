namespace JedPlotUtils.Palette;

public class OutliersHighlight(IPalette Palette)
{
    public Color GetOutlierColor(string code) =>
        code switch
        {
            "AO" => BankPalette.Green,
            "TC" => BankPalette.BlueMedium,
            "LS" => BankPalette.Red,
            "SO" => BankPalette.Magenta,
            _ => BankPalette.BlueAccent
        };

    public Color GetOutlierContrastColor(string code) =>
        Palette.GetContrastColor(GetOutlierColor(code));
}
