using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Detection;

public interface IBossNameDetector
{
    /// <summary>Locates boss HP bars in the bottom boss-bar area (cheap pixel scan, no OCR).</summary>
    IReadOnlyList<BossHealthBarRegion> AnalyzeBars(Bitmap screenshot);

    /// <summary>
    /// Runs OCR only inside each detected bar's name region and validates every candidate against the
    /// boss list. Text outside a bar's name region, and any candidate that does not match the list, is
    /// ignored.
    /// </summary>
    Task<BossNameDetectionResult> ReadBossNamesAsync(
        Bitmap screenshot,
        IReadOnlyList<BossHealthBarRegion> bars,
        BossNameMatcher matcher,
        CancellationToken cancellationToken);
}
