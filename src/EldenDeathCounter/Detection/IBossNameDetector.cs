using System.Drawing;
using EldenDeathCounter.Core.Detection;

namespace EldenDeathCounter.Detection;

public interface IBossNameDetector
{
    /// <summary>
    /// Locates boss HP bars in the bottom boss-bar area (cheap pixel scan, no OCR). The active
    /// <paramref name="gameId"/> selects per-game bar appearance/position tuning (e.g. Dark Souls III's
    /// dim, lower bar and left-shifted name region).
    /// </summary>
    IReadOnlyList<BossHealthBarRegion> AnalyzeBars(Bitmap screenshot, string gameId, string bossHealthBarStyle);

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
