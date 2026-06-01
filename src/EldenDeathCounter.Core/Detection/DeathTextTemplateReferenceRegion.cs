namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Vertical/horizontal crop used to build the death- and boss-victory text templates from
/// reference screenshots. The vertical band is tightened to the measured text ink
/// (~y 0.481..0.553 of a full-screen capture) plus a small margin, so the template stays a
/// subset of the live capture region (<see cref="DeathTextCaptureRegionCalculator"/>) and the
/// matcher keeps enough vertical slide room to absorb the text's on-screen position jitter.
/// </summary>
public static class DeathTextTemplateReferenceRegion
{
    // Shared vertical band for both signals; the measured ink sits at ~0.481..0.553.
    // Kept tight to the ink so the built template stays comfortably smaller than the
    // analyzer search ROI (~0.60 of the capture height), leaving vertical slide room.
    private const double Top = 0.476;
    private const double Bottom = 0.558;

    public static PixelRect DeathScreen(int width, int height)
    {
        return Calculate(width, height, 0.33, 0.67);
    }

    public static PixelRect BossVictory(int width, int height)
    {
        // Boss-victory phrases ("ENEMY FELLED"/"POKONANO WROGA") are wider than "YOU DIED".
        return Calculate(width, height, 0.24, 0.78);
    }

    private static PixelRect Calculate(int width, int height, double left, double right)
    {
        return new PixelRect(
            (int)(width * left),
            (int)(height * Top),
            (int)(width * right),
            (int)(height * Bottom));
    }
}
