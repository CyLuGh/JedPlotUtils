using LanguageExt;

namespace JedPlotUtils.Palette;

public record BankPalette : APalette
{
    public override string Name => "Bank";

    public BankPalette()
    {
        Colors = Seq.create(
            new Color(
                192,
                209,
                217
            ) /* Gray */
            ,
            new(
                0,
                135,
                205
            ) /* Blue */
            ,
            new(
                255,
                203,
                5
            ) /* Yellow */
            ,
            new(
                136,
                192,
                61
            ) /* Green (bright) */
            ,
            new(
                243,
                112,
                33
            ) /* Orange */
            ,
            new(
                165,
                176,
                208
            ) /* Gray (darker) */
            ,
            new(
                226,
                16,
                115
            ) /* Pink */
            ,
            new(0, 175, 194) /* Blue-Teal */
        );
    }
}
