namespace JedPlotUtils.Palette;

public static class Palettes
{
    public static IPalette[] Available { get; } = [new TangoPalette(), new SolarizedPalette()];

    extension(string? name)
    {
        public IPalette ToPalette() =>
            Available.Find(p => p.Name.Equals(name, StringComparison.InvariantCultureIgnoreCase))
                .Match(p => p, () => Available[0]);
    }
}
