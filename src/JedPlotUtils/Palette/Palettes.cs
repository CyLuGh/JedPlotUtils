namespace JedPlotUtils.Palette;

public static class Palettes
{
    public static IPalette[] Available { get; } = [new TangoPalette(), new SolarizedPalette(),new BankPalette(),new LollipopPalette(), new Pastel6Palette(), new Pastel12Palette(), new Professional10Palette()];

    extension(string? name)
    {
        public IPalette ToPalette() =>
            Available.Find(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Match(p => p, () => Available[0]);
    }
}
