using LanguageExt;

namespace JedPlotUtils.Palette;

public record TangoPalette : APalette
{
    public override string Name => "Tango";

    public TangoPalette()
    {
        Colors = Seq.create(
            new Color(252, 175, 61), // Light Orange - 0
            new Color(114, 159, 207), // Light Sky Blue - 1
            new Color(77, 154, 5), // Dark Chameleon - 2
            new Color(173, 127, 168), // Light Plum - 3
            new Color(143, 88, 1), // Dark Chocolate - 4
            new Color(204, 0, 0), // Scarlet Red - 5
            new Color(237, 212, 0), // Butter - 6
            new Color(245, 121, 0), // Orange - 7
            new Color(51, 101, 164), // Sky Blue - 8
            new Color(115, 210, 21), // Chameleon - 9
            new Color(117, 79, 123), // Plum - 10
            new Color(193, 125, 16), // Chocolate - 11
            new Color(164, 0, 0), // Dark Scarlet Red - 12
            new Color(196, 160, 0), // Dark Butter - 13
            new Color(206, 91, 0), // Dark Orange - 14
            new Color(31, 73, 135), // Dark Sky Blue - 15
            new Color(138, 226, 51), // Light Chameleon - 16
            new Color(91, 52, 101), // Dark Plum - 17
            new Color(233, 185, 109), // Light Chocolate - 18
            new Color(239, 40, 40), // Light Scarlet Red - 19
            new Color(252, 233, 78) // Light Butter - 20
        );
    }
}
