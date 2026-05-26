namespace EldenDeathCounter.Core.Detection;

public sealed class TextSignalPixelProfile
{
    private TextSignalPixelProfile(string name, Func<RgbPixel, bool> isTextPixel)
    {
        Name = name;
        IsTextPixel = isTextPixel;
    }

    public string Name { get; }

    public Func<RgbPixel, bool> IsTextPixel { get; }

    public static TextSignalPixelProfile Death { get; } = new("death-red", IsDeathTextPixel);

    public static TextSignalPixelProfile BossVictory { get; } = new("boss-victory-gold", IsBossVictoryTextPixel);

    private static bool IsDeathTextPixel(RgbPixel pixel)
    {
        var strongestNonRed = Math.Max(pixel.G, pixel.B);
        return pixel.R >= 70 &&
               pixel.R - strongestNonRed >= 20 &&
               pixel.R >= pixel.G * 1.35 &&
               pixel.R >= pixel.B * 1.20;
    }

    private static bool IsBossVictoryTextPixel(RgbPixel pixel)
    {
        return pixel.R >= 120 &&
               pixel.G >= 85 &&
               pixel.B <= 170 &&
               pixel.R >= pixel.B * 1.30 &&
               pixel.G >= pixel.B * 1.08 &&
               pixel.R >= pixel.G * 0.85 &&
               pixel.R <= pixel.G * 1.65 &&
               ((pixel.R + pixel.G) / 2) - pixel.B >= 35;
    }
}
