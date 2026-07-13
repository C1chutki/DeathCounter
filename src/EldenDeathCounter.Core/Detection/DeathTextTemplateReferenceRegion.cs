namespace EldenDeathCounter.Core.Detection;

/// <summary>
/// Vertical/horizontal crop used to build the death- and boss-victory text templates from
/// reference screenshots. The vertical band is tightened to the measured text ink
/// (~y 0.481..0.553 of a full-screen capture) plus a small margin, so the template stays a
/// subset of the live capture region (<see cref="DeathTextCaptureRegionCalculator"/>) and the
/// matcher keeps enough vertical slide room to absorb the text's on-screen position jitter.
///
/// A reference image may instead be a <em>pre-cropped strip</em> — the live capture ROI itself
/// (e.g. 1690x216 for a 2560x1440 screen) saved to disk rather than a whole screenshot. Cropping
/// the full-screen central band out of such a strip would keep only a thin sliver of the letters,
/// so a strip is detected by its wide aspect ratio and the band is taken at strip-relative
/// fractions instead (see <see cref="StripTop"/>/<see cref="StripBottom"/>).
/// </summary>
public static class DeathTextTemplateReferenceRegion
{
    // Shared vertical band for both signals; the measured ink sits at ~0.481..0.553.
    // Kept tight to the ink so the built template stays comfortably smaller than the
    // analyzer search ROI (~0.60 of the capture height), leaving vertical slide room.
    private const double Top = 0.476;
    private const double Bottom = 0.558;

    // A reference image whose width:height ratio is at least this is treated as a pre-cropped
    // death/victory-text strip rather than a full-screen capture. Full-screen captures are ~16:9
    // (~1.78); a capture-ROI strip is ~7.8:1.
    private const double StripAspectThreshold = 3.0;

    // Text-band fractions within a pre-cropped strip (the strip == the capture ROI). Derived by
    // expressing the full-screen band relative to the capture ROI; these stay stable across 16:9
    // resolutions, so the template built from a strip matches the one built from a full screenshot.
    private const double StripTop = 0.22;
    private const double StripBottom = 0.77;

    // Dark Souls II's "YOU DIED" reference crop sits lower (y ~0.64..0.80) and narrower to the centered
    // text than Elden Ring's mid-screen band; other games keep the shared band.
    private const double DarkSouls2Top = 0.64;
    private const double DarkSouls2Bottom = 0.80;

    public static PixelRect DeathScreen(int width, int height) => DeathScreen(width, height, gameId: null);

    public static PixelRect DeathScreen(int width, int height, string? gameId)
    {
        if (string.Equals(gameId?.Trim(), "DarkSouls2", StringComparison.OrdinalIgnoreCase) &&
            !IsPreCroppedStrip(width, height))
        {
            return new PixelRect(
                (int)(width * 0.30),
                (int)(height * DarkSouls2Top),
                (int)(width * 0.70),
                (int)(height * DarkSouls2Bottom));
        }

        return IsPreCroppedStrip(width, height)
            ? Strip(width, height, 0.24, 0.76)
            : Calculate(width, height, 0.33, 0.67);
    }

    public static PixelRect BossVictory(int width, int height)
    {
        // Boss-victory phrases ("ENEMY FELLED"/"POKONANO WROGA") are wider than "YOU DIED".
        return IsPreCroppedStrip(width, height)
            ? Strip(width, height, 0.10, 0.92)
            : Calculate(width, height, 0.24, 0.78);
    }

    private static bool IsPreCroppedStrip(int width, int height) =>
        height > 0 && (double)width / height >= StripAspectThreshold;

    private static PixelRect Strip(int width, int height, double left, double right)
    {
        return new PixelRect(
            (int)(width * left),
            (int)(height * StripTop),
            (int)(width * right),
            (int)(height * StripBottom));
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
