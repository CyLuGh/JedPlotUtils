namespace JedPlotUtils.Palette;

public static class OutliersHighlight
{
    public static Color GetOutlierColor(string code) =>
        code switch
        {
            "AO" => BankPalette.Green,
            "TC" => BankPalette.BlueMedium,
            "LS" => BankPalette.Red,
            "SO" => BankPalette.Magenta,
            _ => BankPalette.BlueAccent
        };

    public static Color GetOutlierContrastColor(string code) =>
        BankPalette.GetContrastColor(GetOutlierColor(code));
}
